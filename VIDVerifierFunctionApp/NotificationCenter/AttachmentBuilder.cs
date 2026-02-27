namespace VIDVerifier.NotificationCenter;

public class AttachmentBuilder
{
    private readonly List<Block> _blocks = [];

    public AttachmentBuilder AddTextBlock(string text, string? weight = null, string? size = null)
    {
        _blocks.Add(new TextBlock
        {
            Type = "TextBlock",
            Text = text,
            Weight = weight,
            Size = size
        });
        return this;
    }

    public AttachmentBuilder AddImageBlock(string url, string? altText = null)
    {
        _blocks.Add(new ImageBlock
        {
            Type = "Image",
            Url = url,
            AltText = altText
        });
        return this;
    }

    public AttachmentBuilder WithTitle(string title)
    {
        return AddTextBlock(title, "Bolder", "Large");
    }

    public AttachmentBuilder WithDescription(string description)
    {
        return AddTextBlock(description);
    }

    public Attachment Build()
    {
        return new Attachment
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = new AttachmentContent
            {
                Type = "AdaptiveCard",
                Body = _blocks,
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Version = "1.0"
            }
        };
    }
}
