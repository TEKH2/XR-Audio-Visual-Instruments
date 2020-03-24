using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using EXP;
using EXP.XR;

namespace EXP.Painter
{
    public class PainterDebugUI : MonoBehaviour
    {
        public TextMeshProUGUI _DebugText;
        public TextMeshProUGUI _DebugTextController;
        public Brush _Brush;

        void Update()
        {
            if (_Brush.Painting)
                _DebugText.text = "Painting";
            else
                _DebugText.text = "Not Painting";

            _DebugTextController.text = XRControllers.Instance._RightControllerFeatures._XRFloatDict[XRFloats.Trigger].Value.ToString();
        }
    }
}
