using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CardLayout : LayoutGroup
{
    public int rows;
    public int columns;

    public Vector2 spacing;

    public Vector2 cardSize;

    public int preferredTopPadding;

    public override void CalculateLayoutInputVertical()
    {
        if (rows == 0 || columns == 0){
            rows = 4;
            columns = 4;
        }
        float parentWidth = rectTransform.rect.width;
        float parentHeight = rectTransform.rect.height;

        float cellHeight = (parentHeight - 2 * preferredTopPadding - spacing.y * (rows - 1)) / rows;
        float cellWidth = cellHeight;

        cardSize = new Vector2(cellWidth, cellHeight);
        padding.left = Mathf.FloorToInt((parentWidth - columns * cellWidth - spacing.x * (columns - 1)) / 2);
        padding.top = Mathf.FloorToInt((parentHeight - rows * cellHeight - spacing.y * (rows - 1)) / 2);
        padding.bottom = padding.top;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowCount = i / columns;
            int columnCount = i % columns;

            var item = rectChildren[i];

            var xPos = padding.left + cardSize.x * columnCount + spacing.x * (columnCount);
            var yPos = padding.top + cardSize.y * rowCount + spacing.y * (rowCount);

            SetChildAlongAxis(item, 0, xPos, cardSize.x);
            SetChildAlongAxis(item, 1, yPos, cardSize.y);
        }
    }
    
    public override void SetLayoutHorizontal()
    {
        return;
    }

    public override void SetLayoutVertical()
    {
        return;
    }
}
