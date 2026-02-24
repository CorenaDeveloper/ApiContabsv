namespace ApiContabsv.Services
{
    /// <summary>
    /// IHostedService que ejecuta la retransmisión de contingencia cada X minutos.
    /// Equivalente al RetransmissionJob + gocron de api-dte-crediExpress (Go).
    ///
    /// Usa SemaphoreSlim para evitar ejecuciones concurrentes (igual que atomic.Bool en Go).
    /// Usa IServiceScopeFactory porque dteContext es Scoped y este servicio es Singleton.
    /// </summary>
    public class ContingencyRetransmissionJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContingencyRetransmissionJob> _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public ContingencyRetransmissionJob(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<ContingencyRetransmissionJob> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalMinutes = _configuration.GetValue<int>("ContingencySettings:IntervalMinutes", 30);
            _logger.LogInformation(
                "ContingencyRetransmissionJob iniciado. Intervalo: {Min} minutos.", intervalMinutes);

            // Pausa inicial de 2 min para que la app termine de arrancar
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (IsWithinOperatingHours())
                    await RunJob(stoppingToken);

                try { await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("ContingencyRetransmissionJob detenido.");
        }

        private async Task RunJob(CancellationToken stoppingToken)
        {
            // Evitar concurrencia si el ciclo anterior todavía corre
            if (!await _lock.WaitAsync(0))
            {
                _logger.LogWarning("Job ya en ejecución, saltando ciclo.");
                return;
            }

            try
            {
                _logger.LogInformation("Iniciando ciclo contingencia {Time}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // Crear scope porque ContingencyService depende de dteContext (Scoped)
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IContingencyService>();

                // Timeout máximo de 10 minutos por ciclo
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromMinutes(10));

                await service.RetransmitPendingDocuments();

                _logger.LogInformation("Ciclo contingencia completado {Time}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Job de contingencia cancelado por timeout (10 min).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ciclo de contingencia.");
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Controla la ventana horaria. Si no hay configuración, siempre ejecuta.
        /// Ejemplo producción: StartTime=22:00 EndTime=05:00
        /// Ejemplo pruebas:    StartTime=08:00 EndTime=17:00
        /// </summary>
        private bool IsWithinOperatingHours()
        {
            var start = _configuration.GetValue<string>("ContingencySettings:StartTime");
            var end = _configuration.GetValue<string>("ContingencySettings:EndTime");

            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
                return true;

            if (!TimeSpan.TryParse(start, out var startTime) || !TimeSpan.TryParse(end, out var endTime))
                return true;

            var now = DateTime.Now.TimeOfDay;

            // Ventana que cruza medianoche ej: 22:00 → 05:00
            if (startTime > endTime)
                return now >= startTime || now <= endTime;

            // Ventana normal ej: 08:00 → 17:00
            return now >= startTime && now <= endTime;
        }
    }
}