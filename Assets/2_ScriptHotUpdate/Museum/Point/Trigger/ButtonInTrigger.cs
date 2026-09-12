
using UnityEngine;
using UnityEngine.UI;



namespace HotUpdate.Point
{


    [RequireComponent(typeof(Button))]
    public class ButtonInTrigger : TriggerBase
    {
        [SerializeField]
        protected Button button;

       

        public override void Initialize(int subsequence)
        {

            base.Initialize(subsequence);

            button = button == null ? gameObject.GetComponent<Button>() : button;
            button.onClick.AddListener(OnClick);

     
        }



        private void OnClick()
        {
            OnTrigger?.Invoke(InputRoutingSignal.Click, gameObject, subsequence);
        }
    }

}
