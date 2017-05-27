using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetCreateCluster
{
    class DeploymentHelper
    {
        string subscriptionId = "";
        string clientId = "";
        string clientSecret = "";
        string resourceGroupName = "";
        string deploymentName = "";//这个是部署的名字而不是集群的名字，集群的名字是在模板指定的
        string resourceGroupLocation = ""; // 如果是创建新的资源组则需要指定，否则不需要
        string pathToTemplateFile = "template.json";
        string pathToParameterFile = "parameters.json";
        string tenantId = "";

        public async void Run()
        {
            // Try to obtain the service credentials
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, clientSecret);

            // Read the template and parameter file contents
            JObject templateFileContents = GetJsonFileContents(pathToTemplateFile);
            JObject parameterFileContents = GetJsonFileContents(pathToParameterFile);

            // Create the resource manager client
            var resourceManagementClient = new ResourceManagementClient(serviceCreds);
            resourceManagementClient.SubscriptionId = subscriptionId;

            // Create or check that resource group exists
            EnsureResourceGroupExists(resourceManagementClient, resourceGroupName, resourceGroupLocation);

            // Start a deployment
            DeployTemplate(resourceManagementClient, resourceGroupName, deploymentName, templateFileContents, parameterFileContents);
        }

        /// <summary>
        /// Reads a JSON file from the specified path
        /// </summary>
        /// <param name="pathToJson">The full path to the JSON file</param>
        /// <returns>The JSON file contents</returns>
        private JObject GetJsonFileContents(string pathToJson)
        {
            JObject templatefileContent = new JObject();
            using (StreamReader file = File.OpenText(pathToJson))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    templatefileContent = (JObject)JToken.ReadFrom(reader);
                    return templatefileContent;
                }
            }
        }

        /// <summary>
        /// Ensures that a resource group with the specified name exists. If it does not, will attempt to create one.
        /// </summary>
        /// <param name="resourceManagementClient">The resource manager client.</param>
        /// <param name="resourceGroupName">The name of the resource group.</param>
        /// <param name="resourceGroupLocation">The resource group location. Required when creating a new resource group.</param>
        private static void EnsureResourceGroupExists(ResourceManagementClient resourceManagementClient, string resourceGroupName, string resourceGroupLocation)
        {
            if (resourceManagementClient.ResourceGroups.CheckExistence(resourceGroupName) != true)
            {
                Console.WriteLine(string.Format("Creating resource group '{0}' in location '{1}'", resourceGroupName, resourceGroupLocation));
                var resourceGroup = new ResourceGroup();
                resourceGroup.Location = resourceGroupLocation;
                resourceManagementClient.ResourceGroups.CreateOrUpdate(resourceGroupName, resourceGroup);
            }
            else
            {
                Console.WriteLine(string.Format("Using existing resource group '{0}'", resourceGroupName));
            }
        }

        /// <summary>
        /// Starts a template deployment.
        /// </summary>
        /// <param name="resourceManagementClient">The resource manager client.</param>
        /// <param name="resourceGroupName">The name of the resource group.</param>
        /// <param name="deploymentName">The name of the deployment.</param>
        /// <param name="templateFileContents">The template file contents.</param>
        /// <param name="parameterFileContents">The parameter file contents.</param>
        private static void DeployTemplate(ResourceManagementClient resourceManagementClient, string resourceGroupName, string deploymentName, JObject templateFileContents, JObject parameterFileContents)
        {
            Console.WriteLine(
                $"Starting template deployment '{deploymentName}' in resource group '{resourceGroupName}'");
            var deployment = new Deployment();

            deployment.Properties = new DeploymentProperties
            {
                Mode = DeploymentMode.Incremental,
                Template = templateFileContents,
                Parameters = parameterFileContents["parameters"].ToObject<JObject>()
            };

            var deploymentResult = resourceManagementClient.Deployments.CreateOrUpdate(resourceGroupName, deploymentName, deployment);
            Console.WriteLine($"Deployment status: {deploymentResult.Properties.ProvisioningState}");
        }
    }
}
