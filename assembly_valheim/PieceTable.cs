using System;
using System.Collections.Generic;
using UnityEngine;

public class PieceTable : MonoBehaviour
{
	public void UpdateAvailable(HashSet<string> knownRecipies, Player player, bool hideUnavailable, bool noPlacementCost)
	{
		if (this.m_availablePieces.Count == 0)
		{
			for (int i = 0; i < 4; i++)
			{
				this.m_availablePieces.Add(new List<Piece>());
			}
		}
		foreach (List<Piece> list in this.m_availablePieces)
		{
			list.Clear();
		}
		foreach (GameObject gameObject in this.m_pieces)
		{
			Piece component = gameObject.GetComponent<Piece>();
			if (noPlacementCost || (knownRecipies.Contains(component.m_name) && component.m_enabled && (!hideUnavailable || player.HaveRequirements(component, Player.RequirementMode.CanAlmostBuild))))
			{
				if (component.m_category == Piece.PieceCategory.All)
				{
					for (int j = 0; j < 4; j++)
					{
						this.m_availablePieces[j].Add(component);
					}
				}
				else
				{
					this.m_availablePieces[(int)component.m_category].Add(component);
				}
			}
		}
	}

	public GameObject GetSelectedPrefab()
	{
		Piece selectedPiece = this.GetSelectedPiece();
		if (selectedPiece)
		{
			return selectedPiece.gameObject;
		}
		return null;
	}

	public Piece GetPiece(int category, Vector2Int p)
	{
		if (this.m_availablePieces[category].Count == 0)
		{
			return null;
		}
		int num = p.y * 10 + p.x;
		if (num < 0 || num >= this.m_availablePieces[category].Count)
		{
			return null;
		}
		return this.m_availablePieces[category][num];
	}

	public Piece GetPiece(Vector2Int p)
	{
		return this.GetPiece((int)this.m_selectedCategory, p);
	}

	public bool IsPieceAvailable(Piece piece)
	{
		using (List<Piece>.Enumerator enumerator = this.m_availablePieces[(int)this.m_selectedCategory].GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current == piece)
				{
					return true;
				}
			}
		}
		return false;
	}

	public Piece GetSelectedPiece()
	{
		Vector2Int selectedIndex = this.GetSelectedIndex();
		return this.GetPiece((int)this.m_selectedCategory, selectedIndex);
	}

	public int GetAvailablePiecesInCategory(Piece.PieceCategory cat)
	{
		return this.m_availablePieces[(int)cat].Count;
	}

	public List<Piece> GetPiecesInSelectedCategory()
	{
		return this.m_availablePieces[(int)this.m_selectedCategory];
	}

	public int GetAvailablePiecesInSelectedCategory()
	{
		return this.GetAvailablePiecesInCategory(this.m_selectedCategory);
	}

	public Vector2Int GetSelectedIndex()
	{
		return this.m_selectedPiece[(int)this.m_selectedCategory];
	}

	public void SetSelected(Vector2Int p)
	{
		this.m_selectedPiece[(int)this.m_selectedCategory] = p;
	}

	public void LeftPiece()
	{
		if (this.m_availablePieces[(int)this.m_selectedCategory].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.m_selectedCategory];
		int x = vector2Int.x - 1;
		vector2Int.x = x;
		if (vector2Int.x < 0)
		{
			vector2Int.x = 9;
		}
		this.m_selectedPiece[(int)this.m_selectedCategory] = vector2Int;
	}

	public void RightPiece()
	{
		if (this.m_availablePieces[(int)this.m_selectedCategory].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.m_selectedCategory];
		int x = vector2Int.x + 1;
		vector2Int.x = x;
		if (vector2Int.x >= 10)
		{
			vector2Int.x = 0;
		}
		this.m_selectedPiece[(int)this.m_selectedCategory] = vector2Int;
	}

	public void DownPiece()
	{
		if (this.m_availablePieces[(int)this.m_selectedCategory].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.m_selectedCategory];
		int y = vector2Int.y + 1;
		vector2Int.y = y;
		if (vector2Int.y >= 5)
		{
			vector2Int.y = 0;
		}
		this.m_selectedPiece[(int)this.m_selectedCategory] = vector2Int;
	}

	public void UpPiece()
	{
		if (this.m_availablePieces[(int)this.m_selectedCategory].Count <= 1)
		{
			return;
		}
		Vector2Int vector2Int = this.m_selectedPiece[(int)this.m_selectedCategory];
		int y = vector2Int.y - 1;
		vector2Int.y = y;
		if (vector2Int.y < 0)
		{
			vector2Int.y = 4;
		}
		this.m_selectedPiece[(int)this.m_selectedCategory] = vector2Int;
	}

	public void NextCategory()
	{
		if (!this.m_useCategories)
		{
			return;
		}
		this.m_selectedCategory++;
		if (this.m_selectedCategory == Piece.PieceCategory.Max)
		{
			this.m_selectedCategory = Piece.PieceCategory.Misc;
		}
	}

	public void PrevCategory()
	{
		if (!this.m_useCategories)
		{
			return;
		}
		this.m_selectedCategory--;
		if (this.m_selectedCategory < Piece.PieceCategory.Misc)
		{
			this.m_selectedCategory = Piece.PieceCategory.Furniture;
		}
	}

	public void SetCategory(int index)
	{
		if (!this.m_useCategories)
		{
			return;
		}
		this.m_selectedCategory = (Piece.PieceCategory)index;
		this.m_selectedCategory = (Piece.PieceCategory)Mathf.Clamp((int)this.m_selectedCategory, 0, 3);
	}

	public const int m_gridWidth = 10;

	public const int m_gridHeight = 5;

	public List<GameObject> m_pieces = new List<GameObject>();

	public bool m_useCategories = true;

	public bool m_canRemovePieces = true;

	[NonSerialized]
	private List<List<Piece>> m_availablePieces = new List<List<Piece>>();

	[NonSerialized]
	public Piece.PieceCategory m_selectedCategory;

	[NonSerialized]
	public Vector2Int[] m_selectedPiece = new Vector2Int[5];
}
