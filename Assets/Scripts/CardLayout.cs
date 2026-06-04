using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CardLayout : LayoutGroup
{
    public int rows;
    public int columns;

    public Vector2 cardSize;

    public override void CalculateLayoutInputVertical()
    {
        if (rows == 0 || columns == 0){
            rows = 4;
            columns = 4;
        }
        float parentWidth = rectTransform.rect.width;
        float parentHeight = rectTransform.rect.height;

        float cellHeight = parentHeight / rows;
        float cellWidth = cellHeight;

        cardSize = new Vector2(cellWidth, cellHeight);

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowCount = i / columns;
            int columnCount = i % columns;

            var item = rectChildren[i];

            var xPos = cardSize.x * columnCount;
            var yPos = cardSize.y * rowCount;

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
