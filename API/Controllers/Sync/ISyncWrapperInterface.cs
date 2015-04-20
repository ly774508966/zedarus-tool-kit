﻿using UnityEngine;
using System;
using System.Collections;
using Zedarus.Traffico.Data.PlayerData;

namespace Zedarus.ToolKit.API
{
	public interface ISyncWrapperInterface
	{
		#region Events
		event Action SyncFinished;
		#endregion
		
		#region Controls
		void Sync();
		bool SetData<T>(T data);
		T GetData<T>();
		#endregion
	}
}