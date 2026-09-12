using HotUpdate.Attributes;
using HotUpdate.Point;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace HotUpdate.Functions
{

    [Serializable]
    public class MatFlashFunction : FunctionBase
    {


        [SerializeField]
        protected List<GameObject> objs;

        protected List<Material> materials = new();

        protected List<Color> originalEmission = new();
        public override void Initialize(GameObject obj)
        {
            base.Initialize(obj);


            foreach (var item in objs)
            {
                MeshRenderer renderer = item.GetComponent<MeshRenderer>();

                materials.Add(renderer.material);
            }


            foreach (var item in materials)
            {
                if (item.HasProperty("_EmissionColor"))
                {
                    originalEmission.Add(item.GetColor(
                            "_EmissionColor"));

                }
            }
        }

        protected override FunctionDataBase Get_FunctionData()
        {
            return base.Get_FunctionData();
        }


        public override void Execution(InteractionData interactionData)
        {
            base.Execution(interactionData);
        }



        public override void Enter(InteractionData interactionData = null)
        {
           

            foreach (var item in materials)
            {
                item.EnableKeyword(
              "_EMISSION");


                item.SetColor(
                    "_EmissionColor",
                    Color.red * 3f);

            }

        }

        public override void Exit(InteractionData interactionData = null)
        {
            

            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].SetColor(
            "_EmissionColor",
            originalEmission[i]);
            }


        }

    }

}
