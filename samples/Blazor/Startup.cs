using Microsoft.AspNetCore.Components.Builder;

namespace BlazorApp
{
    public class Startup
    {
        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
