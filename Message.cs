using System;

public class Message
{
    public long MessageId { get; set; }
    public string Text { get; set; }
    public string ImagePath { get; set; }
    public bool? IsForPosting { get; set; } = false;
    public Message(long messageId, string text, string imagePath, bool isForPosting)
    {
        MessageId = messageId;
        Text = text;
        ImagePath = imagePath;
        IsForPosting = isForPosting;
    }
}
