namespace FunkoApi.Back.Services.Email;

public class EmailBackgroundWorker(
    EmailQueueService queue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EmailBackgroundWorker> logger) : BackgroundService {

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken) {

        logger.LogInformation("Email worker iniciado");

        while (!stoppingToken.IsCancellationRequested) {
            try {
                var email = await queue.DequeueAsync(stoppingToken);

                if (email is not null) {
                    using var scope = serviceScopeFactory.CreateScope();

                    var emailService = scope.ServiceProvider
                        .GetRequiredService<IEmailService>();

                    await emailService.SendAsync(email, stoppingToken);

                    logger.LogInformation(
                        "Email procesado: {To}", email.To);
                }
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                logger.LogError(
                    ex, "Error procesando email de la cola");
            }
        }

        logger.LogInformation("Email worker detenido");
    }
}