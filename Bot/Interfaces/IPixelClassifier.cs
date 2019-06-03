﻿namespace FishingFun
{
    public interface IPixelClassifier
    {
        bool IsMatch(byte red, byte green, byte blue);

        double ColourMultiplier { get; set; }

        double ColourClosenessMultiplier { get; set; }
    }
}