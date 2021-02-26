using System;
using System.Collections.Generic;
using UnityEngine;

public class PointGenerator
{
	public PointGenerator(int amount, float gridSize)
	{
		this.m_amount = amount;
		this.m_gridSize = gridSize;
	}

	public void Update(Vector3 center, float radius, List<Vector3> newPoints, List<Vector3> removedPoints)
	{
		Vector2Int grid = this.GetGrid(center);
		if (this.m_currentCenterGrid == grid)
		{
			newPoints.Clear();
			removedPoints.Clear();
			return;
		}
		int num = Mathf.CeilToInt(radius / this.m_gridSize);
		if (this.m_currentCenterGrid != grid || this.m_currentGridWith != num)
		{
			this.RegeneratePoints(grid, num);
		}
	}

	private void RegeneratePoints(Vector2Int centerGrid, int gridWith)
	{
		this.m_currentCenterGrid = centerGrid;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		this.m_points.Clear();
		for (int i = centerGrid.y - gridWith; i <= centerGrid.y + gridWith; i++)
		{
			for (int j = centerGrid.x - gridWith; j <= centerGrid.x + gridWith; j++)
			{
				UnityEngine.Random.InitState(j + i * 100);
				Vector3 gridPos = this.GetGridPos(new Vector2Int(j, i));
				for (int k = 0; k < this.m_amount; k++)
				{
					Vector3 item = new Vector3(UnityEngine.Random.Range(gridPos.x - this.m_gridSize, gridPos.x + this.m_gridSize), UnityEngine.Random.Range(gridPos.z - this.m_gridSize, gridPos.z + this.m_gridSize));
					this.m_points.Add(item);
				}
			}
		}
		UnityEngine.Random.state = state;
	}

	public Vector2Int GetGrid(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + this.m_gridSize / 2f) / this.m_gridSize);
		int y = Mathf.FloorToInt((point.z + this.m_gridSize / 2f) / this.m_gridSize);
		return new Vector2Int(x, y);
	}

	public Vector3 GetGridPos(Vector2Int grid)
	{
		return new Vector3((float)grid.x * this.m_gridSize, 0f, (float)grid.y * this.m_gridSize);
	}

	private int m_amount;

	private float m_gridSize = 8f;

	private Vector2Int m_currentCenterGrid = new Vector2Int(99999, 99999);

	private int m_currentGridWith;

	private List<Vector3> m_points = new List<Vector3>();
}
