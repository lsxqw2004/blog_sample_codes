using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hyak.Common;
using Microsoft.Azure.Management.HDInsight.Job;
using Microsoft.Azure.Management.HDInsight.Job.Models;

namespace SubmitNet
{
    class Program
    {
        private static HDInsightJobManagementClient _hdiJobManagementClient;

        private const string ExistingClusterUri = "xxx.azurehdinsight.net";
        private const string ExistingClusterUsername = "admin";
        private const string ExistingClusterPassword = "";

        private const string DefaultStorageAccountName = ""; 
        private const string DefaultStorageAccountKey = ""; 
        private const string DefaultStorageContainerName = ""; 

        static void Main(string[] args)
        {
            Console.WriteLine("The application is running ...");

            var clusterCredentials = new BasicAuthenticationCloudCredentials { Username = ExistingClusterUsername, Password = ExistingClusterPassword };
            _hdiJobManagementClient = new HDInsightJobManagementClient(ExistingClusterUri, clusterCredentials);

            SubmitMRJob();

            Console.WriteLine("Press ENTER to continue ...");
            Console.ReadLine();
        }

        private static void SubmitMRJob()
        {
            //var paras = new MapReduceStreamingJobSubmissionParameters
            //{
            //    Files = new List<string>() { "/example/app/.exe", "/example/apps/wc.exe" },
            //    Mapper = "cat.exe",
            //    Reducer = "wc.exe",
            //    Input= "/example/data/gutenberg/davinci.txt",
            //    Output = "/example/data/StreamingOutput/wc.txt"
            //};

            var paras = new MapReduceStreamingJobSubmissionParameters
            {
                
                Files = new List<string>()
                {
                    "/example/coreapp",
                },
                Mapper = "dotnet coreapp/NetCoreMapper.dll",
                Reducer = "dotnet coreapp/NetCoreReducer.dll",
                Input = "/example/data/gutenberg/davinci.txt",
                Output = "/example/data/StreamingOutput/wc.txt"

            };

            Console.WriteLine("Submitting the MR job to the cluster...");
            var jobResponse = _hdiJobManagementClient.JobManagement.SubmitMapReduceStreamingJob(paras);
            var jobId = jobResponse.JobSubmissionJsonResponse.Id;
            Console.WriteLine("Response status code is " + jobResponse.StatusCode);
            Console.WriteLine("JobId is " + jobId);

            Console.WriteLine("Waiting for the job completion ...");

            // Wait for job completion
            var jobDetail = _hdiJobManagementClient.JobManagement.GetJob(jobId).JobDetail;
            while (!jobDetail.Status.JobComplete)
            {
                Thread.Sleep(1000);
                jobDetail = _hdiJobManagementClient.JobManagement.GetJob(jobId).JobDetail;
            }

            // Get job output
            var storageAccess = new AzureStorageAccess(DefaultStorageAccountName, DefaultStorageAccountKey,
                DefaultStorageContainerName);
            var output = (jobDetail.ExitValue == 0)
                ? _hdiJobManagementClient.JobManagement.GetJobOutput(jobId, storageAccess) // fetch stdout output in case of success
                : _hdiJobManagementClient.JobManagement.GetJobErrorLogs(jobId, storageAccess); // fetch stderr output in case of failure

            Console.WriteLine("Job output is: ");

            using (var reader = new StreamReader(output, Encoding.UTF8))
            {
                string value = reader.ReadToEnd();
                Console.WriteLine(value);
            }
        }
    }
}
