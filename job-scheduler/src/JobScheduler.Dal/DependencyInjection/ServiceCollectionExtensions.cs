using Azure.Core;
using Azure.Identity;
using JobScheduler.Dal.Common;
using JobScheduler.Dal.Connections;
using JobScheduler.Dal.JobDetails;
using JobScheduler.Dal.JobOrchestrator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JobScheduler.Dal.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobSchedulerDal(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        DapperConfig.EnsureConfigured();

        services.AddOptions<JobSchedulerDalOptions>().Bind(configuration.GetSection(JobSchedulerDalOptions.SectionName));

        services.TryAddSingleton<TokenCredential>(_ =>
            new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true }));
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddSingleton<AzureSqlTokenProvider>();
        services.TryAddSingleton<ISqlConnectionFactory<JobDetailsDb>, SqlConnectionFactory<JobDetailsDb>>();
        services.TryAddSingleton<ISqlConnectionFactory<JobOrchestratorDb>, SqlConnectionFactory<JobOrchestratorDb>>();

        services.TryAddSingleton<IJobRepository, JobRepository>();
        services.TryAddSingleton<IJobScheduleSnapshotRepository, JobScheduleSnapshotRepository>();
        services.TryAddSingleton<IJobRunRepository, JobRunRepository>();

        return services;
    }
}
