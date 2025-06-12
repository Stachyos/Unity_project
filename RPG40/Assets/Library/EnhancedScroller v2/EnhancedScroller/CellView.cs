using EnhancedUI.EnhancedScroller;

namespace GameLogic
{
    public class CellView : EnhancedScrollerCellView
    {
        protected CellData cellData;
        public virtual void SetData(CellData data)
        {
            this.cellData = data;
        }
    }
}