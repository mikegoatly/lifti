public class Book
{
    public int BookId { get; set; }
    public string Title { get; set; }
    public string[] Authors { get; set; }
    public string Synopsis { get; set; }
    public BookExtract[] Extracts { get; set; }
}

public class BookExtract
{
    public BookExtract(string text)
    {
        this.Text = text;
    }

    public string Text { get; }
}