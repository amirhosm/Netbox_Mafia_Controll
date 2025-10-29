#define RTL_OVERRIDE

using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace RTLTMPro
{
    [ExecuteInEditMode]
    public class RTLTextMeshPro : TextMeshProUGUI
    {
        // ReSharper disable once InconsistentNaming
#if RTL_OVERRIDE
        public override string text
#else
        public new string text
#endif
        {
            get { return base.text; }
            set
            {
                if (originalText == value)
                    return;

                originalText = value;

                UpdateText();
            }
        }

        public string OriginalText
        {
            get { return originalText; }
        }

        public bool ForceFix
        {
            get { return forceFix; }
            set
            {
                if (forceFix == value)
                    return;

                forceFix = value;
                havePropertiesChanged = true;
            }
        }

        [SerializeField]
        protected bool preserveNumbers;

        [SerializeField]
        protected bool farsi = true;

        [SerializeField]
        [TextArea(3, 10)]
        protected string originalText;

        [SerializeField]
        protected bool fixTags = true;

        public bool forceFix;

        protected bool checkedEn;

        protected override void OnDisable()
        {
            base.OnDisable();
            checkedEn = false;
        }
        protected void Update()
        {
            if (havePropertiesChanged)
            {
                UpdateText();
            }


            //if (!checkedEn && Application.IsPlaying(gameObject) && Translate.isEn)
            //{
            //    gameObject.GetComponent<RTLTextMeshPro>().font = Resources.Load("sdf", typeof(TMP_FontAsset)) as TMP_FontAsset;
            //    gameObject.GetComponent<RTLTextMeshPro>().preserveNumbers = true;
            //    checkedEn = true;
            //    UpdateText();

            //    //Debug.Log(gameObject.GetComponent<RTLTextMeshPro>().originalText);
            //    if (Translate.allDict.ContainsKey(gameObject.GetComponent<RTLTextMeshPro>().originalText))
            //    {
            //        gameObject.GetComponent<RTLTextMeshPro>().text = Translate.allDict[gameObject.GetComponent<RTLTextMeshPro>().originalText];
            //    }
            //    else
            //    {
            //        foreach (KeyValuePair<string, string> pair in Translate.DynamicList)
            //        {
            //            if (gameObject.GetComponent<RTLTextMeshPro>().originalText.Contains(pair.Key))
            //            {
            //                //Debug.Log("XXXXXXXXXXXXXXXXXXXXXXXXXX " + pair.Key);
            //                gameObject.GetComponent<RTLTextMeshPro>().text = gameObject.GetComponent<RTLTextMeshPro>().originalText.Replace(pair.Key, pair.Value);
            //                //gameObject.GetComponent<RTLTextMeshPro>().text = "";
            //            }
            //        }
            //    }
            //}
        }

        public void UpdateText()
        {
            if (originalText == null)
                originalText = "";

            if (ForceFix == false && RTLSupport.IsRTLInput(originalText) == false)
            {
                isRightToLeftText = false;
                base.text = originalText;
            }
            else
            {
                isRightToLeftText = true;
                base.text = GetFixedText(originalText);
            }

            havePropertiesChanged = true;
        }

        private string GetFixedText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = RTLSupport.FixRTL(input, fixTags, preserveNumbers, farsi);
            input = input.Reverse().ToArray().ArrayToString();

            return input;
        }
    }

}