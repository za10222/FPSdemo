using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorTest :MonoBehaviour
{

	// Use this for initialization
	/// <summary>
	/// �������
	/// </summary>
	public static void ToHideCursor()
	{
		Cursor.visible = false;
	}
	/// <summary>
	/// ��ʾ���
	/// </summary>
	public static void ToShowCursor() 
	{
		Cursor.visible = true;
	}
}
