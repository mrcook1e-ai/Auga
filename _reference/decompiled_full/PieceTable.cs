using System;
using System.Collections.Generic;
using UnityEngine;

public class PieceTable : MonoBehaviour
{
	public const int m_gridWidth = 15;

	public const int m_gridHeight = 6;

	public List<GameObject> m_pieces = new List<GameObject>();

	public List<Piece.PieceCategory> m_categories = new List<Piece.PieceCategory>();

	public List<string> m_categoryLabels = new List<string>();

	public bool m_canRemovePieces = true;

	public bool m_canRemoveFeasts;

	public Skills.SkillType m_skill;

	[NonSerialized]
	private List<List<Piece>> m_availablePieces = new List<List<Piece>>();

	private Piece.PieceCategory m_selectedCategory = Piece.PieceCategory.Max;

	[NonSerialized]
	public Vector2Int[] m_selectedPiece = new Vector2Int[8];

	[NonSerialized]
	public Vector2Int[] m_lastSelectedPiece = new Vector2Int[8];

	[HideInInspector]
	public List<Piece.PieceCategory> m_categoriesFolded = new List<Piece.PieceCategory>();

	public void UpdateAvailable(HashSet<string> knownRecipies, Player player, bool hideUnavailable, bool noPlacementCost)
	{
		if (m_availablePieces.Count == 0)
		{
			for (int i = 0; i < 8; i++)
			{
				m_availablePieces.Add(new List<Piece>());
			}
		}
		foreach (List<Piece> availablePiece in m_availablePieces)
		{
			availablePiece.Clear();
		}
		foreach (GameObject piece in m_pieces)
		{
			Piece component = piece.GetComponent<Piece>();
			bool flag = player.CurrentSeason != null && player.CurrentSeason.Pieces.Contains(piece);
			if ((!noPlacementCost || component.m_canRockJade) && (!knownRecipies.Contains(component.m_name) || !(component.m_enabled || flag) || (hideUnavailable && !player.HaveRequirements(component, Player.RequirementMode.CanAlmostBuild))))
			{
				continue;
			}
			if (component.m_category == Piece.PieceCategory.All)
			{
				for (int j = 0; j < 8; j++)
				{
					m_availablePieces[j].Add(component);
				}
			}
			else
			{
				m_availablePieces[(int)component.m_category].Add(component);
			}
		}
	}

	public GameObject GetSelectedPrefab()
	{
		Piece selectedPiece = GetSelectedPiece();
		if ((bool)selectedPiece)
		{
			return selectedPiece.gameObject;
		}
		return null;
	}

	public Piece GetPiece(Piece.PieceCategory category, Vector2Int p)
	{
		if (m_availablePieces.Count == 0)
		{
			return null;
		}
		int num = (int)category;
		if (num >= m_availablePieces.Count)
		{
			num = 0;
		}
		if (m_availablePieces[num].Count == 0)
		{
			return null;
		}
		int num2 = p.y * 15 + p.x;
		if (num2 < 0 || num2 >= m_availablePieces[num].Count)
		{
			return null;
		}
		return m_availablePieces[num][num2];
	}

	public Piece GetPiece(Vector2Int p)
	{
		return GetPiece(GetSelectedCategory(), p);
	}

	public bool IsPieceAvailable(Piece piece)
	{
		foreach (Piece item in m_availablePieces[(int)GetSelectedCategory()])
		{
			if (item == piece)
			{
				return true;
			}
		}
		return false;
	}

	public Piece.PieceCategory GetSelectedCategory()
	{
		if (m_selectedCategory == Piece.PieceCategory.Max)
		{
			if (m_categories.Count == 0)
			{
				return Piece.PieceCategory.Misc;
			}
			m_selectedCategory = m_categories[0];
		}
		return m_selectedCategory;
	}

	public Piece GetSelectedPiece()
	{
		Vector2Int selectedIndex = GetSelectedIndex();
		return GetPiece(GetSelectedCategory(), selectedIndex);
	}

	public int GetAvailablePiecesInCategory(Piece.PieceCategory cat)
	{
		return m_availablePieces[Mathf.Min(m_availablePieces.Count - 1, (int)cat)].Count;
	}

	public List<Piece> GetPiecesInSelectedCategory()
	{
		return m_availablePieces[(int)GetSelectedCategory()];
	}

	public int GetAvailablePiecesInSelectedCategory()
	{
		return GetAvailablePiecesInCategory(GetSelectedCategory());
	}

	public Vector2Int GetSelectedIndex()
	{
		return m_selectedPiece[Mathf.Min((int)GetSelectedCategory(), m_selectedPiece.Length - 1)];
	}

	public bool GetPieceIndex(Piece p, out Vector2Int index, out int category)
	{
		string prefabName = Utils.GetPrefabName(p.gameObject);
		for (int i = 0; i < m_availablePieces.Count; i++)
		{
			for (int j = 0; j < m_availablePieces[i].Count; j++)
			{
				Piece piece = m_availablePieces[i][j];
				if (!(Utils.GetPrefabName(piece.gameObject) == prefabName))
				{
					continue;
				}
				category = -1;
				for (int k = 0; k < m_categories.Count; k++)
				{
					if (piece.m_category == m_categories[k])
					{
						category = k;
						break;
					}
				}
				if (category >= 0)
				{
					index = new Vector2Int(j % 15, (j - j % 15) / 15);
					return true;
				}
				index = Vector2Int.zero;
				return false;
			}
		}
		index = Vector2Int.zero;
		category = -1;
		return false;
	}

	public void SetSelected(Vector2Int p)
	{
		m_selectedPiece[(int)GetSelectedCategory()] = p;
	}

	public void LeftPiece()
	{
		if (m_availablePieces[(int)GetSelectedCategory()].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)GetSelectedCategory()];
			int x = vector2Int.x - 1;
			vector2Int.x = x;
			if (vector2Int.x < 0)
			{
				vector2Int.x = 14;
			}
			m_selectedPiece[(int)GetSelectedCategory()] = vector2Int;
		}
	}

	public void RightPiece()
	{
		if (m_availablePieces[(int)GetSelectedCategory()].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)GetSelectedCategory()];
			int x = vector2Int.x + 1;
			vector2Int.x = x;
			if (vector2Int.x >= 15)
			{
				vector2Int.x = 0;
			}
			m_selectedPiece[(int)GetSelectedCategory()] = vector2Int;
		}
	}

	public void DownPiece()
	{
		if (m_availablePieces[(int)GetSelectedCategory()].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)GetSelectedCategory()];
			int y = vector2Int.y + 1;
			vector2Int.y = y;
			if (vector2Int.y >= 6)
			{
				vector2Int.y = 0;
			}
			m_selectedPiece[(int)GetSelectedCategory()] = vector2Int;
		}
	}

	public void UpPiece()
	{
		if (m_availablePieces[(int)GetSelectedCategory()].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)GetSelectedCategory()];
			int y = vector2Int.y - 1;
			vector2Int.y = y;
			if (vector2Int.y < 0)
			{
				vector2Int.y = 5;
			}
			m_selectedPiece[(int)GetSelectedCategory()] = vector2Int;
		}
	}

	public void NextCategory()
	{
		if (m_categories.Count == 0)
		{
			return;
		}
		for (int num = m_categories.Count - 1; num >= 0; num--)
		{
			if (m_categories[num] == GetSelectedCategory())
			{
				if (num + 1 == m_categories.Count)
				{
					m_selectedCategory = m_categories[0];
				}
				else
				{
					m_selectedCategory = m_categories[num + 1];
				}
				break;
			}
		}
	}

	public void PrevCategory()
	{
		if (m_categories.Count == 0)
		{
			return;
		}
		for (int i = 0; i < m_categories.Count; i++)
		{
			if (m_categories[i] == GetSelectedCategory())
			{
				if (i - 1 < 0)
				{
					m_selectedCategory = m_categories[m_categories.Count - 1];
				}
				else
				{
					m_selectedCategory = m_categories[i - 1];
				}
				break;
			}
		}
	}

	public void SetCategory(int index)
	{
		if (m_categories.Count != 0)
		{
			m_selectedCategory = m_categories[index];
		}
	}
}
