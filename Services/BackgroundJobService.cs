using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// D?ch v? Background Job s? d?ng Hangfire (và fallback nh? khi Hangfire không có)
    /// </summary>
    public interface IBackgroundJobService
    {
        string ScheduleEmailJob(string email, string subject, string body, TimeSpan delay);
        void ScheduleRecurringJob<T>(string jobId, string cronExpression) where T : IRecurringJob;
        void CancelJob(string jobId);
    }

    public interface IRecurringJob
    {
        void Execute();
    }

    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly IEmailService _emailService;

        public BackgroundJobService(ILogger<BackgroundJobService> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public string ScheduleEmailJob(string email, string subject, string body, TimeSpan delay)
        {
            try
            {
                // If Hangfire is available (JobStorage.Current != null) use BackgroundJob.Schedule
                if (JobStorage.Current != null)
                {
                    var jobId = BackgroundJob.Schedule(
                        () => Console.WriteLine($"Sending email to {email}"),
                        delay);

                    _logger.LogInformation("Email job scheduled: {JobId}", jobId);
                    return jobId;
                }

                // Fallback: schedule a local delayed task to send email (best-effort)
                var fallbackId = "fallback-" + Guid.NewGuid().ToString();
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delay);
                        await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
                        _logger.LogInformation("(Fallback) Email sent to {Email}", email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "(Fallback) Failed to send email to {Email}", email);
                    }
                });

                _logger.LogInformation("Fallback email job scheduled: {FallbackId}", fallbackId);
                return fallbackId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling email job: {Message}", ex.Message);
                throw;
            }
        }

        public void ScheduleRecurringJob<T>(string jobId, string cronExpression) where T : IRecurringJob
        {
            try
            {
                if (JobStorage.Current != null)
                {
                    RecurringJob.AddOrUpdate<T>(jobId, x => x.Execute(), cronExpression);
                    _logger.LogInformation("Recurring job '{JobId}' scheduled with cron expression: {Cron}", jobId, cronExpression);
                }
                else
                {
                    _logger.LogWarning("Unable to schedule recurring job '{JobId}' because Hangfire is not available.", jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling recurring job: {Message}", ex.Message);
            }
        }

        public void CancelJob(string jobId)
        {
            try
            {
                if (JobStorage.Current != null)
                {
                    BackgroundJob.Delete(jobId);
                    _logger.LogInformation("Job '{JobId}' cancelled", jobId);
                }
                else
                {
                    _logger.LogWarning("Unable to cancel job '{JobId}' because Hangfire is not available.", jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job: {Message}", ex.Message);
            }
        }
    }
}
