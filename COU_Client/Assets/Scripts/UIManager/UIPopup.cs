public abstract class UIPopup : UIView
{
    public override void Open()
    {
        base.Open();
        transform.SetAsLastSibling();
        UIManager.Instance.PushPopup(this);
    }

    public override void Close()
    {
        base.Close();
        UIManager.Instance.RemovePopup(this);
    }
}
