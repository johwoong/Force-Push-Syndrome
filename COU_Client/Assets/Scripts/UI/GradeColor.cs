using UnityEngine;

public static class GradeColor
{
    private static readonly Color[] colors =
    {
        new Color32(0xAF, 0xBC, 0xCD, 0xFF),
        new Color32(0x00, 0xC4, 0x5D, 0xFF),
        new Color32(0x07, 0x8C, 0xE4, 0xFF)
    };

    public static Color Get(ItemGrade grade)
        => colors[(int)grade];
}
