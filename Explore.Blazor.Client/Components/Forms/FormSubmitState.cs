namespace Explore.Blazor.Client.Components.Forms;

public class FormSubmitState
{
    public bool IsSubmitting { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void Start()
    {
        IsSubmitting = true;
        IsSuccess = false;
        ErrorMessage = null;
    }

    public void Complete()
    {
        IsSubmitting = false;
        IsSuccess = true;
        ErrorMessage = null;
    }

    public void Fail(string error)
    {
        IsSubmitting = false;
        IsSuccess = false;
        ErrorMessage = error;
    }

    public void Reset()
    {
        IsSubmitting = false;
        IsSuccess = false;
        ErrorMessage = null;
    }
}
