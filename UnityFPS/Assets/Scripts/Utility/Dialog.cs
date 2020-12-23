using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Dialog : MonoBehaviour
{
	public Text Text_title, Text_content, Text_contentNTitle, Text_ok, Text_cancel;
	public Button Button_OK, Button_Cancel, Button_close;
	public Image Img_btnOK, Img_btnCancel;
	public delegate void CallBack();
	private CallBack callBackOnOK, callBackOnCancel;
	public GameObject Panel_OnlyText, Panel_HaveTittle;
	private static GameObject instance;
	public Sprite Sprite_btnYellow, Sprite_BtnGreen;
	public enum ButtonColor
	{
		yellow, green
	}
	public static Dialog Setup()
	{
		if (instance == null)
		{
			// Create popup and attach it to UI
			instance = Instantiate(Resources.Load("Prefabs/Panel_Dialog") as GameObject);
			GameObject PopupCanvas = GameObject.Find("Canvas");
			instance.transform.SetParent(PopupCanvas.transform);
			instance.transform.localScale = Vector3.one;
			instance.transform.localPosition = Vector3.zero;
			instance.GetComponent<RectTransform>().anchorMin = Vector2.zero;
			instance.GetComponent<RectTransform>().anchorMax = Vector2.one;
			instance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
			instance.transform.SetAsLastSibling();
			Canvas can = instance.AddComponent<Canvas>();
			can.overrideSorting = true;
			can.sortingLayerName = "Tutorial";
			can.sortingOrder = 0;
			instance.AddComponent<GraphicRaycaster>();
			// Configure popup
		}
		//ConfirmBox pScript = instance.GetComponent<ConfirmBox>();
		//pScript.AddMessage(message);
		return instance.GetComponent<Dialog>();
	}

	private void Awake()
	{
		//   Button_OK.onClick.AddListener(OnClickOk);
		//   Button_Cancel.onClick.AddListener(OnClickCancel);
	}
//	// Use this for initialization
//	public void Show(string title, string content,
//		CallBack cbOnOk = null, CallBack cbOnCancel = null,
//		bool showCancel = false, string OKText = "OK", string CancelText = "CANCEL",
//ButtonColor OkBtnColor = ButtonColor.yellow, ButtonColor CancelBtnColor = ButtonColor.yellow)
//	{
//		if (title.Trim().Length == 0) title = "Info";

//		if (title.Trim().Length > 0)
//		{
//			Panel_HaveTittle.SetActive(true);
//			Panel_OnlyText.SetActive(false);
//			Text_title.text = GameData.Instance.LocalizationText(title); ;
//			Text_contentNTitle.text = GameData.Instance.LocalizationText(content);
//		}
//		else
//		{
//			Panel_HaveTittle.SetActive(false);
//			Panel_OnlyText.SetActive(true);
//			Text_content.text = GameData.Instance.LocalizationText(content);
//		}
//		Text_ok.text = GameData.Instance.LocalizationText(OKText);
//		Text_cancel.text = GameData.Instance.LocalizationText(CancelText);
//		Button_close.gameObject.SetActive(showCancel);
//		Button_Cancel.gameObject.SetActive(true);
//		Img_btnOK.sprite = OkBtnColor.Equals(ButtonColor.yellow) ? Sprite_btnYellow : Sprite_BtnGreen;
//		Img_btnCancel.sprite = CancelBtnColor.Equals(ButtonColor.yellow) ? Sprite_btnYellow : Sprite_BtnGreen;
//		//this.Text_title.text = GameData.Instance.LocalizationText(title);
//		//this.Text_content.text = GameData.Instance.LocalizationText(content);
//		this.callBackOnOK = cbOnOk;
//		this.callBackOnCancel = cbOnCancel;
//		instance.gameObject.SetActive(true);
//	}

	//public void ShowConfirm(
	//	string title, string content,
	//	CallBack cbOnOk = null
	//	)
	//{
	//	if (title.Trim().Length <= 0) title = "Info";
	//	if (title.Trim().Length > 0)
	//	{
	//		Panel_HaveTittle.SetActive(true);
	//		Panel_OnlyText.SetActive(false);
	//		Text_title.text = GameData.Instance.LocalizationText(title);
	//		Text_contentNTitle.text = GameData.Instance.LocalizationText(content);
	//	}
	//	else
	//	{
	//		Panel_HaveTittle.SetActive(false);
	//		Panel_OnlyText.SetActive(true);
	//		Text_content.text = GameData.Instance.LocalizationText(content);
	//	}
	//	Text_ok.text = GameData.Instance.LocalizationText("OK");
	//	Img_btnOK.sprite = Sprite_btnYellow;
	//	//	Text_cancel.text = GameData.Instance.LocalizationText(CancelText);
	//	Button_Cancel.gameObject.SetActive(false);
	//	Button_close.gameObject.SetActive(false);
	//	//this.Text_title.text = title;
	//	//this.Text_content.text = content;
	//	if (cbOnOk != null)
	//		this.callBackOnOK = cbOnOk;
	//	else
	//		this.callBackOnOK = () =>
	//		{
	//			this.Hide();
	//		};
	//	this.callBackOnCancel = null;
	//	gameObject.SetActive(true);
	//}

	//public void ShowConfirmNotEnoughtSkillPoint(CallBack cbOnOk = null)
	//{
	//	this.Show("", GameData.Instance.LocalizationText("Not enought Skill Points"), () =>
	//	{
	//		//GameEvent.OnAddPlayerValue.Instance.playerValue = GameEvent.PlayerValue.Diamond;
	//		//GameUtils.RaiseMessage(GameEvent.OnAddPlayerValue.Instance);
	//		Dialog.Setup().Hide();
	//	}, cbOnOk, false, GameData.Instance.LocalizationText("Get more"),
	//GameData.Instance.LocalizationText("OK"), ButtonColor.green, ButtonColor.yellow);
	//}
	//public void ShowConfirmNotEnoughtDiamond(CallBack cbOnOk = null)
	//{
	//	this.Show("", GameData.Instance.LocalizationText("Not enought Diamond"), () =>
	//	{
	//		GameEvent.OnAddPlayerValue.Instance.playerValue = GameEvent.PlayerValue.Diamond;
	//		GameUtils.RaiseMessage(GameEvent.OnAddPlayerValue.Instance);
	//		Dialog.Setup().Hide();
	//	}, cbOnOk, false, GameData.Instance.LocalizationText("Get more"),
 //GameData.Instance.LocalizationText("OK"), ButtonColor.green, ButtonColor.yellow);
	//}
	//public void ShowConfirmNotEnoughtGold(CallBack cbOnOk = null)
	//{
	//	this.Show("", GameData.Instance.LocalizationText("Not enought Gold"), () =>
	//	{
	//		GameEvent.OnAddPlayerValue.Instance.playerValue = GameEvent.PlayerValue.Gold;
	//		GameUtils.RaiseMessage(GameEvent.OnAddPlayerValue.Instance);
	//		Dialog.Setup().Hide();
	//	}, cbOnOk, false, GameData.Instance.LocalizationText("Get more"),
 //GameData.Instance.LocalizationText("OK"), ButtonColor.green, ButtonColor.yellow);
	//}
	//public void ShowConfirmNotEnoughtDust(CallBack cbOnOk = null)
	//{
	//	this.Show("", GameData.Instance.LocalizationText("Not enought Dust"), () =>
	//	{
	//		GameEvent.OnAddPlayerValue.Instance.playerValue = GameEvent.PlayerValue.Dust;
	//		GameUtils.RaiseMessage(GameEvent.OnAddPlayerValue.Instance);
	//		Dialog.Setup().Hide();
	//	}, cbOnOk, false, GameData.Instance.LocalizationText("Get more"),
 //GameData.Instance.LocalizationText("OK"), ButtonColor.green, ButtonColor.yellow);
	//}
	//public void ShowConfirmNotEnoughtHeroPieces(HeroData hero, CallBack cbOnOk = null)
	//{
	//	this.Show("", GameData.Instance.LocalizationText("Not enought Hero Pieces"), () =>
	//	{
	//		ShowHeroPicesGet(hero);
	//		Dialog.Setup().Hide();
	//	}, cbOnOk, false, GameData.Instance.LocalizationText("Get more"),
 //GameData.Instance.LocalizationText("OK"), ButtonColor.green, ButtonColor.yellow);
	//}
	//public static void ShowHeroPicesGet(HeroData hero)
	//{
	//	//GameEvent.OnAddPlayerValue.Instance.playerValue = GameEvent.PlayerValue.HeroPieces;
	//	//GameUtils.RaiseMessage(GameEvent.OnAddPlayerValue.Instance);
	//	ItemData _heroPi = new ItemData();
	//	_heroPi.itemType = ItemType.HeroPieces;
	//	_heroPi.heroData = hero;
	//	SelectSceneController.Instance.SelectNumberItemController.Show(_heroPi, PanelNumberSelectWithItemViewController.NumberViewType.Image, (cardNumberGet) =>
	//	{
	//		//List<ItemData> cardsGot = getRandomHeroCard(cardNumberGet, rank);
	//		ItemData _heroPiget = new ItemData();
	//		_heroPiget.itemType = ItemType.HeroPieces;
	//		_heroPiget.Amount = cardNumberGet;
	//		//	SelectSceneController.Instance.GetItemController.Show(new List<ItemData>() { _heroPiget });
	//		//if (this.OnCompleteHeroSelectEvent != null)
	//		//{
	//		//	this.OnCompleteHeroSelectEvent(null, cardNumberGet);
	//		//}
	//		//Dialog.Setup().Hide();
	//	}, true);
	//}
	public void OnClickOk()
	{
		if (this.callBackOnOK != null)
			this.callBackOnOK();
	}
	public void OnClickCancel()
	{
		if (this.callBackOnCancel != null)
			this.callBackOnCancel();
		instance.gameObject.SetActive(false);
	}
	public void Hide()
	{
		instance.gameObject.SetActive(false);
	}
}
