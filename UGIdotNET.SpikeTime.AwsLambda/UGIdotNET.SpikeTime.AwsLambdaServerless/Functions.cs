using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace UGIdotNET.SpikeTime.AwsLambdaServerless;

public class Functions
{
    /// <summary>
    /// Default constructor that Lambda will invoke.
    /// </summary>
    public Functions()
    {
    }


    /// <summary>
    /// A Lambda function to respond to HTTP Get methods from API Gateway
    /// </summary>
    /// <remarks>
    /// This uses the <see href="https://github.com/aws/aws-lambda-dotnet/blob/master/Libraries/src/Amazon.Lambda.Annotations/README.md">Lambda Annotations</see> 
    /// programming model to bridge the gap between the Lambda programming model and a more idiomatic .NET model.
    /// 
    /// This automatically handles reading parameters from an APIGatewayProxyRequest
    /// as well as syncing the function definitions to serverless.template each time you build.
    /// 
    /// If you do not wish to use this model and need to manipulate the API Gateway 
    /// objects directly, see the accompanying Readme.md for instructions.
    /// </remarks>
    /// <param name="context">Information about the invocation, function, and execution environment</param>
    /// <returns>The response as an implicit <see cref="APIGatewayProxyResponse"/></returns>
    [LambdaFunction(Policies = "AWSLambdaBasicExecutionRole", MemorySize = 512, Timeout = 30)]
    [RestApi(LambdaHttpMethod.Get, "/")]
    public IHttpResult Get(ILambdaContext context)
    {
        context.Logger.LogInformation("Handling the 'Get' Request");

        return HttpResults.Ok(new { message = "Hello Spike Time" });
    }

    [LambdaFunction(Policies = "AWSLambdaBasicExecutionRole", MemorySize = 512, Timeout = 30)]
    [RestApi(LambdaHttpMethod.Post, "/")]
    public IHttpResult Post(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<SpikeTimeRequest>(request.Body);
            if (model is null)
            {
                context.Logger.LogWarning("Invalid model");
                return HttpResults.BadRequest(new { error = "invalid model" });
            }

            context.Logger.LogInformation("Handling the 'Get' Request");
            return HttpResults.Ok(new { message = $"Hello: {model!.Message}" });
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.ToString());
            return HttpResults.InternalServerError(ex);
        }
    }

    internal record SpikeTimeRequest(string Message);
}
