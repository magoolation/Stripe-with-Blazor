using Stripe;
using Stripe.Checkout;
using Stripe.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

StripeConfiguration.ApiKey = app.Configuration.GetValue<string>("StripeSecretKey");

app.MapPost("api/stripehooks", async (HttpRequest request) =>
{
    var json = await new StreamReader(request.Body).ReadToEndAsync();
    var secret = app.Configuration.GetValue<string>("StripeWebHookSecret");

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            request.Headers["Stripe-Signature"],
            secret
        );

        // Handle the checkout.session.completed event
        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            SessionGetOptions options = new SessionGetOptions();
            options.AddExpand("line_items");

            var service = new SessionService();
            // Retrieve the session. If you require line items in the response, you may include them by expanding line_items.
            var sessionWithLineItems = await service.GetAsync(session.Id, options);
            StripeList<LineItem> lineItems = sessionWithLineItems.LineItems;

            // do something here based on the order's line items!
        }

        return Results.Ok();
    }
    catch (StripeException e)
    {
        return Results.BadRequest(e.Message);
    }
});

app.Run();
