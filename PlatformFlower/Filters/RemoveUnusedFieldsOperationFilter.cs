using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PlatformFlower.Filters
{
    public class RemoveUnusedFieldsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var parametersToRemove = new List<OpenApiParameter>();
            foreach (var parameter in operation.Parameters)
            {
                if (parameter.Name == "ContentType" ||
                    parameter.Name == "ContentDisposition" ||
                    parameter.Name == "Headers" ||
                    parameter.Name == "Length")
                {
                    parametersToRemove.Add(parameter);
                }
            }

            foreach (var parameter in parametersToRemove)
            {
                operation.Parameters.Remove(parameter);
            }
        }
    }

}
