using System;
using System.Collections.Generic;
using EnhancedUI;
using EnhancedUI.EnhancedScroller;
using UnityEngine;

namespace GameLogic
{
    public class SimpleScrollerDelegate : MonoBehaviour,IEnhancedScrollerDelegate
    {
        public float cellSize = 100f;
        public EnhancedScrollerCellView cellViewPrefab;
        public EnhancedScroller scroller;
        public SmallList<CellData> cellDatum = new();

        private void Start()
        {
            scroller.Delegate = this;
        }

        public void AddCell(CellData cellData)
        {
            cellDatum.Add(cellData);
        }

        public void RemoveCell(CellData cellData)
        {
            cellDatum.Remove(cellData);
        }

        public void RemoveCellAt(int index)
        {
            cellDatum.RemoveAt(index);
        }

        public void ClearAllCells()
        {
            cellDatum.Clear();
        }

        public void AddCellRange(IEnumerable<CellData> enumerable)
        {
            foreach (var item in enumerable)
            {
                cellDatum.Add(item);
            }
        }

        public void ReloadData()
        {
            scroller.ReloadData();
        }

        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            return cellDatum.Count;
        }

        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            return cellSize;
        }

        public virtual EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
           
            CellView cellView = scroller.GetCellView(cellViewPrefab) as CellView;

            // set the name of the game object to the cell's data index.
            // this is optional, but it helps up debug the objects in 
            // the scene hierarchy.
            cellView.name = "Cell " + dataIndex.ToString();

            // in this example, we just pass the data to our cell's view which will update its UI
            cellView.SetData(cellDatum[dataIndex]);

            // return the cell to the scroller
            return cellView;
        }
    }
}