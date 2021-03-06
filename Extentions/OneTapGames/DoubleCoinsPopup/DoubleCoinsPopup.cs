using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Zedarus.ToolKit.Data.Game;
using Zedarus.ToolKit.Data.Player;
using Zedarus.ToolKit.UI;
using Zedarus.ToolKit.API;
using Zedarus.ToolKit.Localisation;

namespace Zedarus.ToolKit.Extentions.OneTapGames.DoubleCoinsPopup
{
	public class DoubleCoinsPopup : ExtentionUIPopup
	{
		#region Properties
		private DoubleCoinsPopupData _data;
		private Wallet _wallet;
		private bool _newSession = false;
		private bool _openSession = false;
		private string _videoAdID = null;
		private int _sessions = 0;
		private int _additionalCoins = 0;
		private int _coinsEarned = 0;
		private System.Action<bool> _callback;
		private UIManager _ui = null;
		#endregion

		#region Settings
		private const int BUTTON_AGREE = 10;
		private const int BUTTON_CANCEL = 12;
		private const int BUTTON_SUCCESS = 13;
		private const int MESSAGE_SUCCESS = 14;
		#endregion

		#region Init
		public DoubleCoinsPopup(GameData gameData, APIManager apiManager, Wallet wallet, LocalisationManager localisation, string videoAdID, string genericPopupID, 
			object popupHeaderStringID, object popupMessageStringID,
			object agreeButtonLocalisationID, object cancelButtonLocalisationID,
			object successMessageLocalisationID, object successButtonLocalisationID,
			int agreeButtonColorID = 0, int cancelButtonColorID = 0) : base(apiManager, localisation, genericPopupID, popupHeaderStringID, popupMessageStringID)
		{
			_data = gameData.Get<DoubleCoinsPopupData>().First;

			if (_data == null)
			{
				throw new UnityException("No DoubleCoinsPopupData found in game data");
			}

			_wallet = wallet;
			_videoAdID = videoAdID;
			_sessions = 0;

			apiManager.Ads.CacheRewardVideos(_videoAdID, true);

			CreateButtonKeys(BUTTON_AGREE, agreeButtonLocalisationID, agreeButtonColorID);
			CreateButtonKeys(BUTTON_CANCEL, cancelButtonLocalisationID, cancelButtonColorID);
			CreateButtonKeys(BUTTON_SUCCESS, successButtonLocalisationID, 0);

			CreateLocalisationKey(MESSAGE_SUCCESS, successMessageLocalisationID);
		}
		#endregion

		#region Controls
		internal override void RegisterSessionStart()
		{
			_newSession = true;
			_openSession = true;
		}

		internal override void RegisterSessionEnd()
		{
			if (_openSession)
			{
				_openSession = false;
				_sessions++;
			}
		}

		public bool DisplayPopup(UIManager uiManager, int coinsEarned, System.Action<bool> callback, GameObject adBlock)
		{
			if (CanUse(coinsEarned) && _data.Multiplier > 1)
			{
				_ui = uiManager;

				AssignAdBlock(adBlock);
				int multiplier = _data.Multiplier - 1;

				if (multiplier < 0)
				{
					multiplier = 0;
				}

				_coinsEarned = coinsEarned;
				_additionalCoins = coinsEarned * multiplier;

				string header = Localise(POPUP_HEADER);
				string message = Localise(POPUP_MESSAGE);

				if (header != null)
				{
					header = string.Format(header, coinsEarned);
				}

				if (message != null)
				{
					message = string.Format(message, coinsEarned);
				}

				DisplayPopup(uiManager, header, message,
					CreateButton(BUTTON_AGREE, OnDoubleConfirmed, BUTTON_AGREE),
					CreateButton(BUTTON_CANCEL, OnCancel, BUTTON_CANCEL)
				);

				_callback = callback;

				return true;
			}
			else
			{
				_additionalCoins = 0;
				_callback = null;
				return false;
			}
		}

		private bool CanUse(int coinsEarned)
		{
			bool canDisplay = false;

			if (coinsEarned >= _data.MinCoins)
			{
				canDisplay = true;
			}

			int sessions = _sessions - _data.Offset;

			if (sessions < 0)
			{
				sessions = 0;
				canDisplay = false;
			}

			if (sessions % _data.Delay != 0)
			{
				canDisplay = false;
			}

			if (coinsEarned <= 0)
			{
				canDisplay = false;
			}

			if (!_data.Enabled)
			{
				canDisplay = false;
			}

			if (!_newSession)
			{
				canDisplay = false;
			}

			return canDisplay;
		}
		#endregion

		#region Helpers
		private void Use()
		{
			_newSession = false;

			if (_callback != null)
			{
				if (_wallet != null)
				{
					_wallet.Deposit(_additionalCoins);
				}

				if (_ui != null)
				{
					DisplayPopup(_ui, null, string.Format(Localise(MESSAGE_SUCCESS), _coinsEarned + _additionalCoins),
						CreateButton(BUTTON_SUCCESS, OnSuccesClick, BUTTON_SUCCESS)
					);
				}

				_additionalCoins = 0;
				_coinsEarned = 0;
			}
		}

		private void Decline()
		{
			_newSession = false;
			if (_callback != null)
			{
				_callback(false);
				_additionalCoins = 0;
				_callback = null;
			}
		}
		#endregion

		#region Getters
		#endregion

		#region Analytics
		protected override string EventName
		{
			get { return "Double Coins Popup"; }
		}
		#endregion

		#region UI Callbacks
		private void OnSuccesClick()
		{
			if (_callback != null)
			{
				_callback(true);
				_callback = null;
			}
		}

		private void OnDoubleConfirmed()
		{
			LogAnalytics("yes");
			ActivateAdBlock();
			API.Ads.ShowRewardedVideo(_videoAdID, OnSecondChanceRewardVideoClose, OnSecondChanceRewardVideoReward, 0);
		}

		private void OnCancel()
		{
			LogAnalytics("no");

			Decline();
		}

		private void OnSecondChanceRewardVideoClose()
		{
			Zedarus.ToolKit.DelayedCall.Create(WaitAndCheckForReward, 2f);
		}

		private void WaitAndCheckForReward()
		{
			DeactivateAdBlock();
			if (_newSession)
			{
				LogAnalytics("reward - failure");
				Decline();
			}
		}

		private void OnSecondChanceRewardVideoReward(int productID)
		{
			DeactivateAdBlock();
			if (_newSession)
			{
				LogAnalytics("reward - success");
				Use();
			}
		}
		#endregion
	}
}
