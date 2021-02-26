using System;
using System.Collections.Generic;
using UnityEngine;

public class SE_Rested : SE_Stats
{
	public override void Setup(Character character)
	{
		base.Setup(character);
		this.UpdateTTL();
		Player player = this.m_character as Player;
		this.m_character.Message(MessageHud.MessageType.Center, "$se_rested_start ($se_rested_comfort:" + player.GetComfortLevel().ToString() + ")", 0, null);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		this.m_timeSinceComfortUpdate -= dt;
	}

	public override void ResetTime()
	{
		this.UpdateTTL();
	}

	private void UpdateTTL()
	{
		Player player = this.m_character as Player;
		float num = this.m_baseTTL + (float)(player.GetComfortLevel() - 1) * this.m_TTLPerComfortLevel;
		float num2 = this.m_ttl - this.m_time;
		if (num > num2)
		{
			this.m_ttl = num;
			this.m_time = 0f;
		}
	}

	private static int PieceComfortSort(Piece x, Piece y)
	{
		if (x.m_comfortGroup != y.m_comfortGroup)
		{
			return x.m_comfortGroup.CompareTo(y.m_comfortGroup);
		}
		if (x.m_comfort != y.m_comfort)
		{
			return x.m_comfort.CompareTo(y.m_comfort);
		}
		return y.m_name.CompareTo(x.m_name);
	}

	public static int CalculateComfortLevel(Player player)
	{
		if (SE_Rested.ghostLayer == 0)
		{
			SE_Rested.ghostLayer = LayerMask.NameToLayer("ghost");
		}
		List<Piece> nearbyPieces = SE_Rested.GetNearbyPieces(player.transform.position);
		nearbyPieces.Sort(new Comparison<Piece>(SE_Rested.PieceComfortSort));
		int num = 1;
		if (player.InShelter())
		{
			num++;
			int i = 0;
			while (i < nearbyPieces.Count)
			{
				Piece piece = nearbyPieces[i];
				if (i <= 0)
				{
					goto IL_A0;
				}
				Piece piece2 = nearbyPieces[i - 1];
				if (piece2.gameObject.layer != SE_Rested.ghostLayer && (piece.m_comfortGroup == Piece.ComfortGroup.None || piece.m_comfortGroup != piece2.m_comfortGroup) && !(piece.m_name == piece2.m_name))
				{
					goto IL_A0;
				}
				IL_A9:
				i++;
				continue;
				IL_A0:
				num += piece.m_comfort;
				goto IL_A9;
			}
		}
		return num;
	}

	private static List<Piece> GetNearbyPieces(Vector3 point)
	{
		SE_Rested.m_tempPieces.Clear();
		Piece.GetAllPiecesInRadius(point, 10f, SE_Rested.m_tempPieces);
		return SE_Rested.m_tempPieces;
	}

	private static int ghostLayer = 0;

	private static List<Piece> m_tempPieces = new List<Piece>();

	[Header("__SE_Rested__")]
	public float m_baseTTL = 300f;

	public float m_TTLPerComfortLevel = 60f;

	private const float m_comfortRadius = 10f;

	private float m_timeSinceComfortUpdate;
}
