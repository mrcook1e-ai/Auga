public class TaskPopup : FixedPopupBase
{
	public override PopupType Type => PopupType.Task;

	public TaskPopup(string header, string text, bool localizeText = true)
		: base(header, text, localizeText)
	{
	}
}
