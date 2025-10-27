using System;
using UnityEngine;

namespace BladeAction.EditorTools
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class StatLimitAttribute : PropertyAttribute
    {
        public readonly string statKey;
        public readonly bool showAsSlider;

        public StatLimitAttribute(string statKey, bool showAsSlider = true)
        {
            this.statKey = statKey;
            this.showAsSlider = showAsSlider;
        }
    }
}


