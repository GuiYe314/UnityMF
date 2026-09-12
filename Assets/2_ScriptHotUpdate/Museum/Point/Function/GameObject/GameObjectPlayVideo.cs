using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;


namespace HotUpdate.FunctionsPlayVideo
{
    [Serializable]
    public class GameObjectPlayVideo : FunctionBase
    {

        [SerializeField]
        protected VideoPlayer player;
        [SerializeField]
        protected VideoClip videoClip;
        [SerializeField]
        protected bool isPlay;



        public override void Enter(InteractionData interactionData = null)
        {
         
                Play(isPlay);

        }

        public override void Exit(InteractionData interactionData = null)
        {
            
                Play(!isPlay);

        }



        protected void Play(bool isPlay)
        {
         
            player.clip = videoClip;
            if (isPlay)
                player.Play();
            else
                player.Stop();
        }


    }

}

