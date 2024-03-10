using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using AdaptiveCards;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;

namespace M365CopilotPlugin.Search;

public class SearchApp : TeamsActivityHandler
{
    private readonly string _adaptiveCardFilePath = Path.Combine(".", "Resources", "helloWorldCard.json");

    protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
    {
        var templateJson = await System.IO.File.ReadAllTextAsync(_adaptiveCardFilePath, cancellationToken);
        var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);

        var text = query?.Parameters?[0]?.Value as string ?? string.Empty;

        var rate = await FindRate(text);

        var attachments = new List<MessagingExtensionAttachment>();

        string resultCard = template.Expand(rate);

        var previewCard = new HeroCard
        {
            Title = rate.id,
            Subtitle = rate.rateUsd

        }.ToAttachment();

        var attachment = new MessagingExtensionAttachment
        {
            Content = JsonConvert.DeserializeObject(resultCard),
            ContentType = "application/vnd.microsoft.card.adaptive",
            Preview = previewCard,
        };

        attachments.Add(attachment);

        return new MessagingExtensionResponse
        {
            ComposeExtension = new MessagingExtensionResult
            {
                Type = "result",
                AttachmentLayout = "list",
                Attachments = attachments
            }
        };
    }

    private async Task<Rate> FindRate(string currency)
    {
        var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.coincap.io/v2/rates/{currency}");

        var obj = JObject.Parse(response);

        var rateData = JsonConvert.DeserializeObject<Rate>(obj["data"].ToString());
        
        rateData.rateUsd = decimal.Parse(rateData.rateUsd).ToString("C", CultureInfo.GetCultureInfo(1033));

        return rateData;
    }
}

public class Rate
{
    public string id { get; set; }
    public string symbol { get; set; }
    public string currencySymbol { get; set; }
    public string type { get; set; }
    public string rateUsd { get; set; }
}