using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// DTO para creación de facturas electrónicas con validaciones completas
    /// Implementa las mismas validaciones que el sistema GO antes del envío al Ministerio de Hacienda
    /// </summary>
    public class CreateInvoiceRequestDTO : IValidatableObject
    {
        [JsonPropertyName("clientId")]
        [Required(ErrorMessage = "El ID del cliente es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del cliente debe ser mayor a 0")]
        public int ClientId { get; set; }

        [JsonPropertyName("userId")]
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "El ID del usuario debe ser mayor a 0")]
        public int UserId { get; set; }

        [JsonPropertyName("items")]
        [Required(ErrorMessage = "Los items son requeridos")]
        [MinLength(1, ErrorMessage = "Debe incluir al menos 1 item")]
        [MaxLength(2000, ErrorMessage = "El número de items no puede exceder 2000")]
        public List<InvoiceItemRequestDTO> Items { get; set; } = new();

        [JsonPropertyName("receiver")]
        public ReceiverRequestDTO? Receiver { get; set; }

        [JsonPropertyName("modelType")]
        public int? ModelType { get; set; }

        [JsonPropertyName("summary")]
        [Required(ErrorMessage = "El resumen es requerido")]
        public InvoiceSummaryRequestDTO? Summary { get; set; }

        [JsonPropertyName("certificatePassword")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña del certificado debe tener entre 6 y 255 caracteres")]
        public string? CertificatePassword { get; set; }

        [JsonPropertyName("environment")]
        [RegularExpression(@"^(00|01)$", ErrorMessage = "El ambiente debe ser '00' (prueba) o '01' (producción)")]
        public string? Environment { get; set; } = "00";

        [JsonPropertyName("sendToHacienda")]
        public bool? SendToHacienda { get; set; } = true;

        [JsonPropertyName("third_party_sale")]
        public object? ThirdPartySale { get; set; }

        [JsonPropertyName("related_docs")]
        public object[]? RelatedDocs { get; set; }

        [JsonPropertyName("other_docs")]
        public object[]? OtherDocs { get; set; }

        [JsonPropertyName("appendixes")]
        public object[]? Appendixes { get; set; }

        /// <summary>
        /// Validación personalizada que replica la lógica del sistema GO
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validar que todos los items tengan datos válidos
            ValidateItems(results);

            // Validar receptor si es requerido
            ValidateReceiver(results);

            // Validar resumen y totales
            ValidateSummary(results);

            return results;
        }

        private void ValidateItems(List<ValidationResult> results)
        {
            if (Items == null || !Items.Any())
            {
                results.Add(new ValidationResult("Debe incluir al menos 1 item", new[] { nameof(Items) }));
                return;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var prefix = $"Items[{i}]";

                // Validar que la cantidad sea positiva
                if (item.Quantity <= 0)
                {
                    results.Add(new ValidationResult($"La cantidad del item {i + 1} debe ser mayor a 0", new[] { $"{prefix}.Quantity" }));
                }

                // Validar que el precio unitario no sea negativo
                if (item.UnitPrice < 0)
                {
                    results.Add(new ValidationResult($"El precio unitario del item {i + 1} no puede ser negativo", new[] { $"{prefix}.UnitPrice" }));
                }

                // Validar que el descuento no sea negativo
                if (item.Discount < 0)
                {
                    results.Add(new ValidationResult($"El descuento del item {i + 1} no puede ser negativo", new[] { $"{prefix}.Discount" }));
                }

                // Validar que el descuento no sea mayor al subtotal
                var itemSubtotal = item.Quantity * item.UnitPrice;
                if (item.Discount > itemSubtotal)
                {
                    results.Add(new ValidationResult($"El descuento del item {i + 1} no puede ser mayor al subtotal", new[] { $"{prefix}.Discount" }));
                }

                // Validar descripción
                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    results.Add(new ValidationResult($"La descripción del item {i + 1} es requerida", new[] { $"{prefix}.Description" }));
                }
                else if (item.Description.Length > 1000)
                {
                    results.Add(new ValidationResult($"La descripción del item {i + 1} no puede exceder 1000 caracteres", new[] { $"{prefix}.Description" }));
                }

                // Validar tipo de item
                if (item.Type < 1 || item.Type > 4)
                {
                    results.Add(new ValidationResult($"El tipo del item {i + 1} debe estar entre 1 y 4", new[] { $"{prefix}.Type" }));
                }

                // Validar unidad de medida para tipo 4
                if (item.Type == 4 && item.UnitMeasure != 99)
                {
                    results.Add(new ValidationResult($"Para items tipo 4, la unidad de medida debe ser 99", new[] { $"{prefix}.UnitMeasure" }));
                }
            }
        }

        private void ValidateReceiver(List<ValidationResult> results)
        {
            if (Receiver == null) return;

            // Validar documento del receptor
            if (!string.IsNullOrEmpty(Receiver.DocumentType) && !string.IsNullOrEmpty(Receiver.DocumentNumber))
            {
                switch (Receiver.DocumentType)
                {
                    case "13": // DUI
                        if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.DocumentNumber, @"^[0-9]{8}-[0-9]{1}$"))
                        {
                            results.Add(new ValidationResult("El DUI debe tener formato XXXXXXXX-X (8 dígitos, guión, 1 dígito)", new[] { "Receiver.DocumentNumber" }));
                        }
                        break;

                    case "36": // NIT
                        // Limpiar guiones para validación
                        var cleanNit = Receiver.DocumentNumber.Replace("-", "").Trim();
                        if (!System.Text.RegularExpressions.Regex.IsMatch(cleanNit, @"^([0-9]{14}|[0-9]{9})$"))
                        {
                            results.Add(new ValidationResult("El NIT debe tener 14 dígitos (persona jurídica) o 9 dígitos (persona natural)", new[] { "Receiver.DocumentNumber" }));
                        }
                        break;

                    default:
                        // Validación genérica para otros tipos de documento
                        if (Receiver.DocumentNumber.Length < 3 || Receiver.DocumentNumber.Length > 20)
                        {
                            results.Add(new ValidationResult("El número de documento debe tener entre 3 y 20 caracteres", new[] { "Receiver.DocumentNumber" }));
                        }
                        break;
                }
            }

            // Validar email si está presente
            if (!string.IsNullOrEmpty(Receiver.Email))
            {
                var emailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.Email, emailRegex))
                {
                    results.Add(new ValidationResult("El formato del email no es válido", new[] { "Receiver.Email" }));
                }
                else if (Receiver.Email.Length < 3 || Receiver.Email.Length > 100)
                {
                    results.Add(new ValidationResult("El email debe tener entre 3 y 100 caracteres", new[] { "Receiver.Email" }));
                }
            }

            // Validar teléfono si está presente
            if (!string.IsNullOrEmpty(Receiver.Phone))
            {
                var phoneRegex = @"^[0-9\s\-\+\(\)]{7,15}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.Phone, phoneRegex))
                {
                    results.Add(new ValidationResult("El formato del teléfono no es válido", new[] { "Receiver.Phone" }));
                }
            }
        }

        private void ValidateSummary(List<ValidationResult> results)
        {
            if (Summary == null) return;

            // Validar que los totales no sean negativos
            if (Summary.TotalNonSubject < 0)
                results.Add(new ValidationResult("El total no sujeto no puede ser negativo", new[] { "Summary.TotalNonSubject" }));

            if (Summary.TotalExempt < 0)
                results.Add(new ValidationResult("El total exento no puede ser negativo", new[] { "Summary.TotalExempt" }));

            if (Summary.TotalTaxed < 0)
                results.Add(new ValidationResult("El total gravado no puede ser negativo", new[] { "Summary.TotalTaxed" }));

            if (Summary.TotalDiscount < 0)
                results.Add(new ValidationResult("El total de descuentos no puede ser negativo", new[] { "Summary.TotalDiscount" }));

            if (Summary.TotalToPay < 0)
                results.Add(new ValidationResult("El total a pagar no puede ser negativo", new[] { "Summary.TotalToPay" }));

            // Validar coherencia de subtotales
            var calculatedSubTotal = Summary.TotalNonSubject + Summary.TotalExempt + Summary.TotalTaxed;
            if (Math.Abs(Summary.SubTotal - calculatedSubTotal) > 0.01m)
            {
                results.Add(new ValidationResult($"El subtotal ({Summary.SubTotal}) no coincide con la suma de totales ({calculatedSubTotal})", new[] { "Summary.SubTotal" }));
            }

            // Validar condición de operación
            if (Summary.OperationCondition < 1 || Summary.OperationCondition > 3)
            {
                results.Add(new ValidationResult("La condición de operación debe ser 1 (Contado), 2 (Crédito) o 3 (Otro)", new[] { "Summary.OperationCondition" }));
            }

            // Validar tipos de pago si están presentes
            if (Summary.PaymentTypes != null && Summary.PaymentTypes.Any())
            {
                var totalPayments = Summary.PaymentTypes.Sum(p => p.Amount);
                if (Math.Abs(totalPayments - Summary.TotalToPay) > 0.01m)
                {
                    results.Add(new ValidationResult($"El total de pagos ({totalPayments}) no coincide con el total a pagar ({Summary.TotalToPay})", new[] { "Summary.PaymentTypes" }));
                }

                foreach (var payment in Summary.PaymentTypes)
                {
                    if (string.IsNullOrWhiteSpace(payment.Code))
                    {
                        results.Add(new ValidationResult("El código de tipo de pago es requerido", new[] { "Summary.PaymentTypes" }));
                    }
                    if (payment.Amount <= 0)
                    {
                        results.Add(new ValidationResult("El monto del tipo de pago debe ser mayor a 0", new[] { "Summary.PaymentTypes" }));
                    }
                }
            }
        }
    }

    public class SigningResult
    {
        public bool Success { get; set; }
        public string Response { get; set; } = "";
        public string? Error { get; set; }
    }

    /// <summary>
    /// DTO para items de factura con validaciones completas
    /// </summary>
    public class InvoiceItemRequestDTO
    {
        [JsonPropertyName("type")]
        [Required(ErrorMessage = "El tipo de item es requerido")]
        [Range(1, 4, ErrorMessage = "El tipo de item debe estar entre 1 y 4")]
        public int Type { get; set; }

        [JsonPropertyName("description")]
        [Required(ErrorMessage = "La descripción es requerida")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "La descripción debe tener entre 1 y 1000 caracteres")]
        public string Description { get; set; } = "";

        [JsonPropertyName("quantity")]
        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit_measure")]
        [Required(ErrorMessage = "La unidad de medida es requerida")]
        [Range(1, 99, ErrorMessage = "La unidad de medida debe estar entre 1 y 99")]
        public int UnitMeasure { get; set; }

        [JsonPropertyName("unit_price")]
        [Required(ErrorMessage = "El precio unitario es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        [Range(0, double.MaxValue, ErrorMessage = "El descuento no puede ser negativo")]
        public decimal Discount { get; set; }

        [JsonPropertyName("code")]
        [StringLength(25, ErrorMessage = "El código no puede exceder 25 caracteres")]
        public string? Code { get; set; }

        [JsonPropertyName("non_subject_sale")]
        [Range(0, double.MaxValue, ErrorMessage = "La venta no sujeta no puede ser negativa")]
        public decimal NonSubjectSale { get; set; }

        [JsonPropertyName("exempt_sale")]
        [Range(0, double.MaxValue, ErrorMessage = "La venta exenta no puede ser negativa")]
        public decimal ExemptSale { get; set; }

        [JsonPropertyName("taxed_sale")]
        [Range(0, double.MaxValue, ErrorMessage = "La venta gravada no puede ser negativa")]
        public decimal TaxedSale { get; set; }

        [JsonPropertyName("suggested_price")]
        [Range(0, double.MaxValue, ErrorMessage = "El precio sugerido no puede ser negativo")]
        public decimal SuggestedPrice { get; set; }

        [JsonPropertyName("non_taxed")]
        [Range(0, double.MaxValue, ErrorMessage = "El monto no gravado no puede ser negativo")]
        public decimal NonTaxed { get; set; }

        [JsonPropertyName("iva_item")]
        [Range(0, double.MaxValue, ErrorMessage = "El IVA del item no puede ser negativo")]
        public decimal IvaItem { get; set; }
    }

    /// <summary>
    /// DTO para receptor con validaciones de documentos según estándares de El Salvador
    /// </summary>
    public class ReceiverRequestDTO
    {
        [JsonPropertyName("document_type")]
        [RegularExpression(@"^(13|36|02|03|37)$", ErrorMessage = "Tipo de documento debe ser: 13 (DUI), 36 (NIT), 02 (Carnet Residente), 03 (Pasaporte), 37 (Otro)")]
        public string? DocumentType { get; set; }

        [JsonPropertyName("document_number")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "El número de documento debe tener entre 3 y 20 caracteres")]
        public string? DocumentNumber { get; set; }

        [JsonPropertyName("name")]
        [StringLength(250, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 250 caracteres")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public AddressRequestDTO? Address { get; set; }

        [JsonPropertyName("phone")]
        [StringLength(25, MinimumLength = 8, ErrorMessage = "El teléfono debe tener entre 8 a 25  caracteres")]
        [RegularExpression(@"^[0-9\s\-\+\(\)]*$", ErrorMessage = "El teléfono solo puede contener números, espacios, guiones, + y paréntesis")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El email debe tener entre 3 y 100 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// DTO para dirección del receptor
    /// </summary>
    public class AddressRequestDTO
    {
        [JsonPropertyName("department")]
        [Required(ErrorMessage = "El departamento es requerido")]
        [RegularExpression(@"^[0-9]{2}$", ErrorMessage = "El código de departamento debe ser de 2 dígitos")]
        public string Department { get; set; } = "";

        [JsonPropertyName("municipality")]
        [Required(ErrorMessage = "El municipio es requerido")]
        [RegularExpression(@"^[0-9]{2}$", ErrorMessage = "El código de municipio debe ser de 2 dígitos")]
        public string Municipality { get; set; } = "";

        [JsonPropertyName("complement")]
        [Required(ErrorMessage = "El complemento de dirección es requerido")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El complemento debe tener entre 5 y 200 caracteres")]
        public string Complement { get; set; } = "";
    }

    /// <summary>
    /// DTO para resumen de factura con validaciones de totales
    /// </summary>
    public class InvoiceSummaryRequestDTO
    {
        [JsonPropertyName("total_non_subject")]
        [Range(0, double.MaxValue, ErrorMessage = "El total no sujeto no puede ser negativo")]
        public decimal TotalNonSubject { get; set; }

        [JsonPropertyName("total_exempt")]
        [Range(0, double.MaxValue, ErrorMessage = "El total exento no puede ser negativo")]
        public decimal TotalExempt { get; set; }

        [JsonPropertyName("total_taxed")]
        [Range(0, double.MaxValue, ErrorMessage = "El total gravado no puede ser negativo")]
        public decimal TotalTaxed { get; set; }

        [JsonPropertyName("sub_total")]
        [Required(ErrorMessage = "El subtotal es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El subtotal no puede ser negativo")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("non_subject_discount")]
        [Range(0, double.MaxValue, ErrorMessage = "El descuento no sujeto no puede ser negativo")]
        public decimal NonSubjectDiscount { get; set; }

        [JsonPropertyName("exempt_discount")]
        [Range(0, double.MaxValue, ErrorMessage = "El descuento exento no puede ser negativo")]
        public decimal ExemptDiscount { get; set; }

        [JsonPropertyName("taxed_discount")]
        [Range(0, double.MaxValue, ErrorMessage = "El descuento gravado no puede ser negativo")]
        public decimal TaxedDiscount { get; set; }

        [JsonPropertyName("discount_percentage")]
        [Range(0, 100, ErrorMessage = "El porcentaje de descuento debe estar entre 0 y 100")]
        public decimal DiscountPercentage { get; set; }

        [JsonPropertyName("total_discount")]
        [Range(0, double.MaxValue, ErrorMessage = "El total de descuentos no puede ser negativo")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("sub_total_sales")]
        [Range(0, double.MaxValue, ErrorMessage = "El subtotal de ventas no puede ser negativo")]
        public decimal SubTotalSales { get; set; }

        [JsonPropertyName("total_operation")]
        [Required(ErrorMessage = "El total de operación es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El total de operación no puede ser negativo")]
        public decimal TotalOperation { get; set; }

        [JsonPropertyName("total_non_taxed")]
        [Range(0, double.MaxValue, ErrorMessage = "El total no gravado no puede ser negativo")]
        public decimal TotalNonTaxed { get; set; }

        [JsonPropertyName("total_to_pay")]
        [Required(ErrorMessage = "El total a pagar es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El total a pagar no puede ser negativo")]
        public decimal TotalToPay { get; set; }

        [JsonPropertyName("operation_condition")]
        [Required(ErrorMessage = "La condición de operación es requerida")]
        [Range(1, 3, ErrorMessage = "La condición de operación debe ser 1 (Contado), 2 (Crédito) o 3 (Otro)")]
        public int OperationCondition { get; set; }

        [JsonPropertyName("iva_retention")]
        [Range(0, double.MaxValue, ErrorMessage = "La retención de IVA no puede ser negativa")]
        public decimal IvaRetention { get; set; }

        [JsonPropertyName("total_iva")]
        [Range(0, double.MaxValue, ErrorMessage = "El total de IVA no puede ser negativo")]
        public decimal TotalIva { get; set; }

        [JsonPropertyName("payment_types")]
        public List<PaymentTypeRequestDTO>? PaymentTypes { get; set; }
    }

    /// <summary>
    /// DTO para tipos de pago
    /// </summary>
    public class PaymentTypeRequestDTO
    {
        [JsonPropertyName("code")]
        [Required(ErrorMessage = "El código de tipo de pago es requerido")]
        [RegularExpression(@"^[0-9]{2}$", ErrorMessage = "El código debe ser de 2 dígitos")]
        public string Code { get; set; } = "";

        [JsonPropertyName("amount")]
        [Required(ErrorMessage = "El monto es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Amount { get; set; }
    }
}

//{
//    "clientId": 5,
//  "userId": 5,
//  "items": [
//    {
//        "type": 1,
//      "description": "CODO PVC 3/4",
//      "quantity": 12,
//      "unit_measure": 59,
//      "unit_price": 0.65,
//      "discount": 0,
//      "code": "COD1",
//      "non_subject_sale": 0,
//      "exempt_sale": 0,
//      "taxed_sale": 7.8,
//      "suggested_price": 0,
//      "non_taxed": 0,
//      "iva_item": 0.9
//    },
//    {
//        "type": 1,
//      "description": "CODO PVC 1",
//      "quantity": 123,
//      "unit_measure": 59,
//      "unit_price": 0.75,
//      "discount": 0,
//      "code": "COD2",
//      "non_subject_sale": 0,
//      "exempt_sale": 0,
//      "taxed_sale": 92.25,
//      "suggested_price": 0,
//      "non_taxed": 0,
//      "iva_item": 10.61
//    }
//  ],
//  "receiver": {
//        "document_type": "13",
//    "document_number": "05128459-6",
//    "name": "MAURICIO CORENA",
//    "address": {
//            "department": "08",
//    "municipality": "23",
//    "complement": "SOYAPANGO, SAN SALVADOR"
//    },
//    "phone": "6102 2136",
//    "email": "corenaDeveloper@gmail.com"
//  },
//  "summary": {
//        "total_non_subject": 0,
//    "total_exempt": 0,
//    "total_taxed": 100.05,
//    "sub_total": 100.05,
//    "non_subject_discount": 0,
//    "exempt_discount": 0,
//    "taxed_discount": 0,
//    "discount_percentage": 0,
//    "total_discount": 0,
//    "sub_total_sales": 100.05,
//    "total_operation": 100.05,
//    "total_non_taxed": 0,
//    "total_to_pay": 100.05,
//    "operation_condition": 1,
//    "iva_retention": 0,
//    "total_iva": 11.51,
//    "payment_types": [
//      {
//            "code": "01",
//        "amount": 100.05
//      }
//    ]
//  },
//  "environment": "00",
//  "sendToHacienda": true
//}