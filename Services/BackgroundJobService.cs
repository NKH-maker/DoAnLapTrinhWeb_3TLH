using Hangfire;
using Hangfire.Server;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// D?ch v? Background Job s? d?ng Hangfire
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

        public BackgroundJobService(ILogger<BackgroundJobService> logger)
        {
            _logger = logger;
        }

        public string ScheduleEmailJob(string email, string subject, string body, TimeSpan delay)
        {
            try
            {
                var jobId = BackgroundJob.Schedule(
                    () => Console.WriteLine($"Sending email to {email}"),
                    delay);
                
                _logger.LogInformation($"Email job scheduled: {jobId}");
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error scheduling email job: {ex.Message}");
                throw;
            }
        }

        public void ScheduleRecurringJob<T>(string jobId, string cronExpression) where T : IRecurringJob
        {
            try
            {
                RecurringJob.AddOrUpdate<T>(jobId, x => x.Execute(), cronExpression);
                _logger.LogInformation($"Recurring job '{jobId}' scheduled with cron expression: {cronExpression}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error scheduling recurring job: {ex.Message}");
            }
        }

        public void CancelJob(string jobId)
        {
            try
            {
                BackgroundJob.Delete(jobId);
                _logger.LogInformation($"Job '{jobId}' cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling job: {ex.Message}");
            }
        }
    }
}
