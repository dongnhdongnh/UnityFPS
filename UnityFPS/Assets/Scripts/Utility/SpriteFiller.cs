using UnityEngine;
using System.Collections;

public class SpriteFiller
{
    //static Texture2D textTemp;
    static float textWidth, textHeight;
    public static Sprite GetSprite(Texture2D textTemp, float percent)
    {
        //  textTemp = input.texture;
        textWidth = textTemp.width;
        textHeight = textTemp.height * percent;
        return Sprite.Create(textTemp, new Rect(0, 0, textWidth, textHeight), new Vector2(0.5f, 1));

    }
}
