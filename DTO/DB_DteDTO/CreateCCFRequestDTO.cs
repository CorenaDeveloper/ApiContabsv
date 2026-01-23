using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiContabsv.DTO.DB_DteDTO
{
    /// <summary>
    /// DTO para creación de Comprobante de Crédito Fiscal (CCF) con validaciones completas
    /// Implementa las mismas validaciones que el sistema GO antes del envío al Ministerio de Hacienda
    /// </summary>
    public class CreateCCFRequestDTO : IValidatableObject
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
        public List<CCFItemRequestDTO> Items { get; set; } = new();

        [JsonPropertyName("receiver")]
        [Required(ErrorMessage = "El receptor es requerido para CCF")]
        public CCFReceiverRequestDTO? Receiver { get; set; }

        [JsonPropertyName("modelType")]
        public int? ModelType { get; set; } = 1;

        [JsonPropertyName("summary")]
        [Required(ErrorMessage = "El resumen es requerido")]
        public CCFSummaryRequestDTO? Summary { get; set; }

        [JsonPropertyName("certificatePassword")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña del certificado debe tener entre 6 y 255 caracteres")]
        public string? CertificatePassword { get; set; }

        [JsonPropertyName("environment")]
        [RegularExpression(@"^(00|01)$", ErrorMessage = "El ambiente debe ser '00' (prueba) o '01' (producción)")]
        public string? Environment { get; set; } = "00";

        [JsonPropertyName("sendToHacienda")]
        public bool? SendToHacienda { get; set; } = true;

        [JsonPropertyName("third_party_sale")]
        public ThirdPartySaleRequestDTO? ThirdPartySale { get; set; }

        [JsonPropertyName("related_docs")]
        [MaxLength(50, ErrorMessage = "No se pueden incluir más de 50 documentos relacionados")]
        public List<RelatedDocRequestDTO>? RelatedDocs { get; set; }

        [JsonPropertyName("other_docs")]
        public object[]? OtherDocs { get; set; }

        [JsonPropertyName("appendixes")]
        public object[]? Appendixes { get; set; }

        /// <summary>
        /// Validación personalizada que replica la lógica del sistema GO para CCF
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validar que todos los items tengan datos válidos para CCF
            ValidateCCFItems(results);

            // Validar receptor específico para CCF
            ValidateCCFReceiver(results);

            // Validar resumen y totales específicos para CCF
            ValidateCCFSummary(results);

            // Validar documentos relacionados para CCF
            ValidateCCFRelatedDocs(results);

            return results;
        }

        private void ValidateCCFItems(List<ValidationResult> results)
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

                // Validaciones básicas de item
                if (item.Quantity <= 0)
                {
                    results.Add(new ValidationResult($"La cantidad del item {i + 1} debe ser mayor a 0", new[] { $"{prefix}.Quantity" }));
                }

                if (item.UnitPrice < 0)
                {
                    results.Add(new ValidationResult($"El precio unitario del item {i + 1} no puede ser negativo", new[] { $"{prefix}.UnitPrice" }));
                }

                if (item.Discount < 0)
                {
                    results.Add(new ValidationResult($"El descuento del item {i + 1} no puede ser negativo", new[] { $"{prefix}.Discount" }));
                }

                // ✅ VALIDACIONES ESPECÍFICAS DE CCF:

                // CCF: Si hay venta gravada > 0 pero precio unitario = 0, es inválido
                if (item.TaxedSale > 0 && item.UnitPrice == 0)
                {
                    results.Add(new ValidationResult($"El item {i + 1} no puede tener precio unitario 0 cuando hay venta gravada", new[] { $"{prefix}.UnitPrice" }));
                }

                // CCF: Si hay no gravado > 0 pero precio unitario = 0, es inválido
                if (item.NonTaxed > 0 && item.UnitPrice == 0)
                {
                    results.Add(new ValidationResult($"El item {i + 1} no puede tener precio unitario 0 cuando hay monto no gravado", new[] { $"{prefix}.UnitPrice" }));
                }

                // CCF: Validar que el tipo de item sea válido (1-4)
                if (item.Type < 1 || item.Type > 4)
                {
                    results.Add(new ValidationResult($"El tipo del item {i + 1} debe estar entre 1 y 4", new[] { $"{prefix}.Type" }));
                }

                // CCF: Para items tipo 4, unidad de medida debe ser 99
                if (item.Type == 4 && item.UnitMeasure != 99)
                {
                    results.Add(new ValidationResult($"Para items tipo 4, la unidad de medida debe ser 99", new[] { $"{prefix}.UnitMeasure" }));
                }

                // CCF: Validar que no sean negativos los montos de venta
                if (item.NonSubjectSale < 0)
                {
                    results.Add(new ValidationResult($"La venta no sujeta del item {i + 1} no puede ser negativa", new[] { $"{prefix}.NonSubjectSale" }));
                }

                if (item.ExemptSale < 0)
                {
                    results.Add(new ValidationResult($"La venta exenta del item {i + 1} no puede ser negativa", new[] { $"{prefix}.ExemptSale" }));
                }

                if (item.TaxedSale < 0)
                {
                    results.Add(new ValidationResult($"La venta gravada del item {i + 1} no puede ser negativa", new[] { $"{prefix}.TaxedSale" }));
                }

                if (item.NonTaxed < 0)
                {
                    results.Add(new ValidationResult($"El monto no gravado del item {i + 1} no puede ser negativo", new[] { $"{prefix}.NonTaxed" }));
                }

                // CCF: Si hay impuestos, debe haber venta gravada
                if (item.Taxes != null && item.Taxes.Any() && item.TaxedSale == 0)
                {
                    results.Add(new ValidationResult($"El item {i + 1} tiene impuestos pero no tiene venta gravada", new[] { $"{prefix}.TaxedSale" }));
                }

                // CCF: Validar códigos de impuestos válidos
                if (item.Taxes != null)
                {
                    var validTaxCodes = new[] { "20", "C8", "D1", "D5", "A8", "6A", "71" }; // Códigos válidos de El Salvador
                    foreach (var taxCode in item.Taxes)
                    {
                        if (!validTaxCodes.Contains(taxCode))
                        {
                            results.Add(new ValidationResult($"El código de impuesto '{taxCode}' no es válido en el item {i + 1}", new[] { $"{prefix}.Taxes" }));
                        }
                    }
                }
            }
        }

        private void ValidateCCFReceiver(List<ValidationResult> results)
        {
            if (Receiver == null)
            {
                results.Add(new ValidationResult("El receptor es obligatorio para CCF", new[] { nameof(Receiver) }));
                return;
            }

            //  CCF REQUIERE NRC OBLIGATORIAMENTE
            if (string.IsNullOrWhiteSpace(Receiver.Nrc))
            {
                results.Add(new ValidationResult("El NRC es obligatorio para el receptor en CCF", new[] { "Receiver.Nrc" }));
            }
            else
            {
                // Validar formato de NRC (1-8 dígitos)
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.Nrc, @"^[0-9]{1,8}$"))
                {
                    results.Add(new ValidationResult("El NRC debe tener entre 1 y 8 dígitos", new[] { "Receiver.Nrc" }));
                }
            }

            //  CCF REQUIERE NIT OBLIGATORIAMENTE
            if (string.IsNullOrWhiteSpace(Receiver.Nrc))
            {
                results.Add(new ValidationResult("El NIT es obligatorio para el receptor en CCF", new[] { "Receiver.Nit" }));
            }
            else
            {
                // Validar formato de NRC (1-8 dígitos)
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.Nrc, @"^[0-9]{1,8}$"))
                {
                    results.Add(new ValidationResult("El NRC debe tener entre 1 y 8 dígitos", new[] { "Receiver.Nrc" }));
                }
            }

            //  CCF REQUIERE CÓDIGO Y DESCRIPCIÓN DE ACTIVIDAD ECONÓMICA
            if (string.IsNullOrWhiteSpace(Receiver.ActivityCode))
            {
                results.Add(new ValidationResult("El código de actividad económica es obligatorio para el receptor en CCF", new[] { "Receiver.ActivityCode" }));
            }
            else
            {
                // Validar formato de código de actividad (2-6 dígitos)
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.ActivityCode, @"^[0-9]{2,6}$"))
                {
                    results.Add(new ValidationResult("El código de actividad económica debe tener entre 2 y 6 dígitos", new[] { "Receiver.ActivityCode" }));
                }
            }

            if (string.IsNullOrWhiteSpace(Receiver.ActivityDescription))
            {
                results.Add(new ValidationResult("La descripción de actividad económica es obligatoria para el receptor en CCF", new[] { "Receiver.ActivityDescription" }));
            }

            // Validar email si está presente
            if (!string.IsNullOrEmpty(Receiver.Email))
            {
                var emailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.Email, emailRegex))
                {
                    results.Add(new ValidationResult("El formato del email no es válido", new[] { "Receiver.Email" }));
                }
            }

            // Validar teléfono si está presente
            if (!string.IsNullOrEmpty(Receiver.Phone))
            {
                var phoneRegex = @"^[0-9\s\-\+\(\)]{7,25}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(Receiver.Phone, phoneRegex))
                {
                    results.Add(new ValidationResult("El formato del teléfono no es válido (7-25 caracteres)", new[] { "Receiver.Phone" }));
                }
            }
        }

        private void ValidateCCFSummary(List<ValidationResult> results)
        {
            if (Summary == null) return;

            // ✅ CCF: TotalIVA DEBE SER 0 (según validaciones específicas de GO)
            if (Summary.TotalIva != 0)
            {
                results.Add(new ValidationResult("El total de IVA debe ser 0 para CCF", new[] { "Summary.TotalIva" }));
            }

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

            // CCF: Validar montos específicos
            if (Summary.TaxedDiscount < 0)
                results.Add(new ValidationResult("El descuento gravado no puede ser negativo", new[] { "Summary.TaxedDiscount" }));

            if (Summary.IvaPerception < 0)
                results.Add(new ValidationResult("La percepción de IVA no puede ser negativa", new[] { "Summary.IvaPerception" }));

            if (Summary.IvaRetention < 0)
                results.Add(new ValidationResult("La retención de IVA no puede ser negativa", new[] { "Summary.IvaRetention" }));

            if (Summary.IncomeRetention < 0)
                results.Add(new ValidationResult("La retención de renta no puede ser negativa", new[] { "Summary.IncomeRetention" }));

            if (Summary.BalanceInFavor < 0)
                results.Add(new ValidationResult("El saldo a favor no puede ser negativo", new[] { "Summary.BalanceInFavor" }));

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

            // CCF: Validar cálculo de percepción de IVA (debe ser 1% del total gravado si no es 0)
            if (Summary.IvaPerception > 0)
            {
                var expectedPerception = Summary.TotalTaxed * 0.01m;
                if (Math.Abs(Summary.IvaPerception - expectedPerception) > 0.01m)
                {
                    results.Add(new ValidationResult($"La percepción de IVA ({Summary.IvaPerception}) debe ser 1% del total gravado ({expectedPerception})", new[] { "Summary.IvaPerception" }));
                }
            }
        }

        private void ValidateCCFRelatedDocs(List<ValidationResult> results)
        {
            if (RelatedDocs == null || !RelatedDocs.Any()) return;

            // ✅ CCF: Validar tipos de documentos relacionados permitidos
            var validCCFRelatedDocTypes = new[] { "01", "03", "04", "05", "06", "07", "08", "09", "11", "14", "15" };

            foreach (var relatedDoc in RelatedDocs)
            {
                if (!validCCFRelatedDocTypes.Contains(relatedDoc.DocumentType))
                {
                    results.Add(new ValidationResult($"Tipo de documento relacionado '{relatedDoc.DocumentType}' no válido para CCF. Tipos permitidos: {string.Join(", ", validCCFRelatedDocTypes)}", new[] { "RelatedDocs" }));
                }
            }

            // ✅ CCF: Validar que cada item tenga su documento relacionado referenciado
            if (Items != null)
            {
                var relatedDocNumbers = RelatedDocs.Select(rd => rd.DocumentNumber).ToHashSet();

                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (!string.IsNullOrEmpty(item.RelatedDocumentNumber))
                    {
                        if (!relatedDocNumbers.Contains(item.RelatedDocumentNumber))
                        {
                            results.Add(new ValidationResult($"El item {i + 1} referencia un documento relacionado '{item.RelatedDocumentNumber}' que no existe en la lista de documentos relacionados", new[] { $"Items[{i}].RelatedDocumentNumber" }));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// DTO para items de CCF con validaciones específicas
    /// </summary>
    public class CCFItemRequestDTO
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
        [JsonConverter(typeof(QuantityConverter))] // ✅ Redondeo a 8 decimales
        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("unit_measure")]
        [Required(ErrorMessage = "La unidad de medida es requerida")]
        [Range(1, 99, ErrorMessage = "La unidad de medida debe estar entre 1 y 99")]
        public int UnitMeasure { get; set; }

        [JsonPropertyName("unit_price")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El descuento no puede ser negativo")]
        public decimal Discount { get; set; }

        [JsonPropertyName("code")]
        [StringLength(25, ErrorMessage = "El código no puede exceder 25 caracteres")]
        public string? Code { get; set; }

        [JsonPropertyName("non_subject_sale")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "La venta no sujeta no puede ser negativa")]
        public decimal NonSubjectSale { get; set; }

        [JsonPropertyName("exempt_sale")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "La venta exenta no puede ser negativa")]
        public decimal ExemptSale { get; set; }

        [JsonPropertyName("taxed_sale")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "La venta gravada no puede ser negativa")]
        public decimal TaxedSale { get; set; }

        [JsonPropertyName("suggested_price")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El precio sugerido no puede ser negativo")]
        public decimal SuggestedPrice { get; set; }

        [JsonPropertyName("non_taxed")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El monto no gravado no puede ser negativo")]
        public decimal NonTaxed { get; set; }

        [JsonPropertyName("taxes")]
        public List<string>? Taxes { get; set; }

        [JsonPropertyName("related_document_number")]
        [StringLength(50, ErrorMessage = "El número de documento relacionado no puede exceder 50 caracteres")]
        public string? RelatedDocumentNumber { get; set; }
    }

    /// <summary>
    /// DTO para receptor de CCF con validaciones específicas
    /// </summary>
    public class CCFReceiverRequestDTO
    {

        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(250, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 250 caracteres")]
        public string? Name { get; set; }

        [JsonPropertyName("nrc")]
        [Required(ErrorMessage = "El NRC es obligatorio para CCF")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "El NRC debe tener entre 1 y 10 caracteres")]
        [RegularExpression(@"^[0-9]{1,8}$", ErrorMessage = "El NRC debe contener solo números de 1 a 8 dígitos")]
        public string? Nrc { get; set; }

        [JsonPropertyName("nit")]
        [Required(ErrorMessage = "El Nit es obligatorio para CCF")]
        [StringLength(17, MinimumLength = 3, ErrorMessage = "El Nit debe tener entre 3 y 17 caracteres")]
        [RegularExpression(@"^[0-9]{3,17}$", ErrorMessage = "El Nit debe contener solo números de 1 a 8 dígitos")]
        public string? Nit { get; set; }

        [JsonPropertyName("activity_code")]
        [Required(ErrorMessage = "El código de actividad económica es obligatorio para CCF")]
        [RegularExpression(@"^[0-9]{2,6}$", ErrorMessage = "El código de actividad económica debe tener entre 2 y 6 dígitos")]
        public string? ActivityCode { get; set; }

        [JsonPropertyName("activity_description")]
        [Required(ErrorMessage = "La descripción de actividad económica es obligatoria para CCF")]
        [StringLength(150, MinimumLength = 1, ErrorMessage = "La descripción de actividad debe tener entre 1 y 150 caracteres")]
        public string? ActivityDescription { get; set; }

        [JsonPropertyName("address")]
        public AddressRequestDTO? Address { get; set; }

        [JsonPropertyName("phone")]
        [StringLength(25, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 25 caracteres")]
        [RegularExpression(@"^[0-9\s\-\+\(\)]{7,25}$", ErrorMessage = "El teléfono debe tener entre 7-25 caracteres y solo puede contener números, espacios, guiones, + y paréntesis")]
        public string? Phone { get; set; }

        [JsonPropertyName("email")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El email debe tener entre 3 y 100 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// DTO para resumen de CCF con redondeo automático
    /// </summary>
    public class CCFSummaryRequestDTO
    {
        [JsonPropertyName("total_non_subject")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El total no sujeto no puede ser negativo")]
        public decimal TotalNonSubject { get; set; }

        [JsonPropertyName("total_exempt")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El total exento no puede ser negativo")]
        public decimal TotalExempt { get; set; }

        [JsonPropertyName("total_taxed")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El total gravado no puede ser negativo")]
        public decimal TotalTaxed { get; set; }

        [JsonPropertyName("sub_total")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Required(ErrorMessage = "El subtotal es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El subtotal no puede ser negativo")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("non_subject_discount")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El descuento no sujeto no puede ser negativo")]
        public decimal NonSubjectDiscount { get; set; }

        [JsonPropertyName("exempt_discount")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El descuento exento no puede ser negativo")]
        public decimal ExemptDiscount { get; set; }

        [JsonPropertyName("taxed_discount")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El descuento gravado no puede ser negativo")]
        public decimal TaxedDiscount { get; set; }

        [JsonPropertyName("discount_percentage")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, 100, ErrorMessage = "El porcentaje de descuento debe estar entre 0 y 100")]
        public decimal DiscountPercentage { get; set; }

        [JsonPropertyName("total_discount")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El total de descuentos no puede ser negativo")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("sub_total_sales")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El subtotal de ventas no puede ser negativo")]
        public decimal SubTotalSales { get; set; }

        [JsonPropertyName("total_operation")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Required(ErrorMessage = "El total de operación es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El total de operación no puede ser negativo")]
        public decimal TotalOperation { get; set; }

        [JsonPropertyName("total_non_taxed")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El total no gravado no puede ser negativo")]
        public decimal TotalNonTaxed { get; set; }

        [JsonPropertyName("total_to_pay")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Required(ErrorMessage = "El total a pagar es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El total a pagar no puede ser negativo")]
        public decimal TotalToPay { get; set; }

        [JsonPropertyName("operation_condition")]
        [Required(ErrorMessage = "La condición de operación es requerida")]
        [Range(1, 3, ErrorMessage = "La condición de operación debe ser 1 (Contado), 2 (Crédito) o 3 (Otro)")]
        public int OperationCondition { get; set; }

        // CAMPOS ESPECÍFICOS DE CCF
        [JsonPropertyName("iva_perception")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "La percepción de IVA no puede ser negativa")]
        public decimal IvaPerception { get; set; }

        [JsonPropertyName("iva_retention")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "La retención de IVA no puede ser negativa")]
        public decimal IvaRetention { get; set; }

        [JsonPropertyName("income_retention")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "La retención de renta no puede ser negativa")]
        public decimal IncomeRetention { get; set; }

        [JsonPropertyName("balance_in_favor")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El saldo a favor no puede ser negativo")]
        public decimal BalanceInFavor { get; set; }

        [JsonPropertyName("total_iva")]
        [JsonConverter(typeof(MoneyConverter))] 
        public decimal TotalIva { get; set; } = 0; 

        [JsonPropertyName("taxes")]
        public List<TaxRequestDTO>? Taxes { get; set; }

        [JsonPropertyName("payment_types")]
        public List<CCFPaymentTypeRequestDTO>? PaymentTypes { get; set; }
    }

    /// <summary>
    /// DTO para impuestos con redondeo automático
    /// </summary>
    public class TaxRequestDTO
    {
        [JsonPropertyName("code")]
        [Required(ErrorMessage = "El código de impuesto es requerido")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "El código debe tener entre 1 y 10 caracteres")]
        public string Code { get; set; } = "";

        [JsonPropertyName("description")]
        [StringLength(150, ErrorMessage = "La descripción no puede exceder 150 caracteres")]
        public string? Description { get; set; }

        [JsonPropertyName("value")]
        [JsonConverter(typeof(MoneyConverter))] 
        [Range(0, double.MaxValue, ErrorMessage = "El valor del impuesto no puede ser negativo")]
        public decimal Value { get; set; }
    }


    /// <summary>
    /// DTO para tipos de pago en CCF con redondeo automático
    /// </summary>
    public class CCFPaymentTypeRequestDTO
    {
        [JsonPropertyName("code")]
        [Required(ErrorMessage = "El código de tipo de pago es requerido")]
        [RegularExpression(@"^[0-9]{2}$", ErrorMessage = "El código debe ser de 2 dígitos")]
        public string Code { get; set; } = "";

        [JsonPropertyName("amount")]
        [JsonConverter(typeof(MoneyConverter))]
        [Required(ErrorMessage = "El monto es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        public decimal Amount { get; set; }

        [JsonPropertyName("reference")]
        [StringLength(50, ErrorMessage = "La referencia no puede exceder 50 caracteres")]
        public string? Reference { get; set; }

        [JsonPropertyName("term")]
        [Range(1, 999, ErrorMessage = "El plazo debe ser entre 1 y 999")]
        public int? Term { get; set; }

        [JsonPropertyName("period")]
        [RegularExpression(@"^[0-9]{2}$", ErrorMessage = "El período debe ser de 2 dígitos")]
        public string? Period { get; set; }
    }

    /// <summary>
    /// DTO para venta a terceros
    /// </summary>
    public class ThirdPartySaleRequestDTO
    {
        [JsonPropertyName("nit")]
        [Required(ErrorMessage = "El NIT es requerido")]
        [RegularExpression(@"^([0-9]{14}|[0-9]{9})$", ErrorMessage = "El NIT debe tener 14 o 9 dígitos")]
        public string Nit { get; set; } = "";

        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(250, MinimumLength = 1, ErrorMessage = "El nombre debe tener entre 1 y 250 caracteres")]
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// DTO para documentos relacionados
    /// </summary>
    public class RelatedDocRequestDTO
    {
        [JsonPropertyName("document_type")]
        [Required(ErrorMessage = "El tipo de documento es requerido")]
        [RegularExpression(@"^(01|03|04|05|06|07|08|09|11|14|15)$", ErrorMessage = "Tipo de documento no válido para CCF")]
        public string DocumentType { get; set; } = "";

        [JsonPropertyName("generation_type")]
        [Range(1, 2, ErrorMessage = "El tipo de generación debe ser 1 (Electrónico) o 2 (Físico)")]
        public int GenerationType { get; set; } = 1;

        [JsonPropertyName("document_number")]
        [Required(ErrorMessage = "El número de documento es requerido")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "El número debe tener entre 1 y 100 caracteres")]
        public string DocumentNumber { get; set; } = "";

        [JsonPropertyName("emission_date")]
        public DateTime EmissionDate { get; set; }
    }
}

//{
//    "clientId": 6,
//  "userId": 5,
//  "items": [
//    {
//        "type": 1,
//      "description": "Venta gravada",
//      "quantity": 1,
//      "unit_measure": 59,
//      "unit_price": 2000.00,
//      "taxed_sale": 2000.00,
//      "exempt_sale": 0,
//      "non_subject_sale": 0,
//      "taxes": [
//        "20"
//      ]
//    }
//  ],
//  "receiver": {
//        "nrc": "3625871",
//    "nit": "06140912941505",
//    "name": "CLIENTE DE PRUEBA",
//    "commercial_name": "EJEMPLO S.A de S.V",
//    "activity_code": "62010",
//    "activity_description": "Programacion informatica",
//    "address": {
//            "department": "06",
//      "municipality": "22",
//      "complement": "Dirección de Prueba 1, N° 1234"
//    },
//    "phone": "21212828",
//    "email": "cliente@gmail.com"
//  },
//  "summary": {
//        "operation_condition": 1,
//    "total_taxed": 2000.00,
//    "total_exempt": 0,
//    "total_non_taxed": 0,
//    "total_non_subject": 0,
//    "sub_total_sales": 2000.00,
//    "sub_total": 2000.00,
//    "iva_perception": 0,
//    "iva_retention": 0,
//    "income_retention": 0,
//    "total_operation": 2260.00,
//    "total_to_pay": 2260.00,
//    "taxes": [
//      {
//            "code": "20",
//        "description": "IVA 13%",
//        "value": 260.00
//      }
//    ],
//    "payment_types": [
//      {
//            "code": "01",
//        "amount": 2260.00
//      }
//    ]
//  },
//  "environment": "00",
//  "sendToHacienda": true
//}