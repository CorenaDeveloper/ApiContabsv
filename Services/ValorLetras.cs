namespace ApiContabsv.Services
{
    public class ValorLetras
    {
        private static readonly string[] Unidades = {
            "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
            "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE",
            "DIECIOCHO", "DIECINUEVE", "VEINTE", "VEINTIÚN", "VEINTIDÓS", "VEINTITRÉS",
            "VEINTICUATRO", "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
        };

        private static readonly string[] Decenas = {
            "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA",
            "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
        };

        private static readonly string[] Centenas = {
            "", "CIEN", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
            "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

        private static string ConvertirCentenas(int numero)
        {
            if (numero == 0) return "";

            var centena = numero / 100;
            var resto = numero % 100;

            string resultado = "";

            if (centena == 1)
                resultado = resto == 0 ? "CIEN" : "CIENTO"; 
            else if (centena > 1)
                resultado = Centenas[centena];

            if (resto > 0)
            {
                if (centena > 0) resultado += " ";
                resultado += ConvertirDecenas(resto);
            }

            return resultado;
        }


        private static string ConvertirDecenas(int numero)
        {
            if (numero < 30) return Unidades[numero];

            var decena = numero / 10;
            var unidad = numero % 10;

            if (unidad == 0) return Decenas[decena];
            return $"{Decenas[decena]} Y {Unidades[unidad]}";
        }

        private static string ConvertirMiles(int numero)
        {
            if (numero == 0) return "";
            if (numero == 1000) return "MIL";

            var miles = numero / 1000;
            var resto = numero % 1000;

            var resultado = "";

            if (miles == 1)
                resultado = "MIL";
            else
                resultado = $"{ConvertirCentenas(miles)} MIL";

            if (resto > 0)
                resultado += $" {ConvertirCentenas(resto)}";

            return resultado;
        }

        private static string ConvertirMillones(int numero)
        {
            if (numero == 0) return "CERO";

            var millones = numero / 1000000;
            var resto = numero % 1000000;

            var resultado = "";

            if (millones == 1)
                resultado = "UN MILLÓN";
            else if (millones > 1)
                resultado = $"{ConvertirCentenas(millones)} MILLONES";

            if (resto >= 1000)
                resultado += (resultado != "" ? " " : "") + ConvertirMiles(resto);
            else if (resto > 0)
                resultado += (resultado != "" ? " " : "") + ConvertirCentenas(resto);

            return resultado != "" ? resultado : ConvertirMiles(numero);
        }

        public static string Convertir(decimal monto, string moneda = "DÓLARES")
        {
            if (monto < 0)
                return "MENOS " + Convertir(-monto, moneda);

            var entero = (int)Math.Floor(monto);
            var centavos = (int)Math.Round((monto - entero) * 100);

            var letrasEntero = entero == 0 ? "CERO" : ConvertirMillones(entero);

            var letrasCentavos = centavos.ToString("D2");

            return $"{letrasEntero} {moneda} CON {letrasCentavos}/100";
        }
    }
}