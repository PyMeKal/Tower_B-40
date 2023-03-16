using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    void Update()
    {

    }



    public void RequestAnimation<T>(string param, T value)
    {
        string valString = value.ToString();
        switch(typeof(T).ToString())
        {
            case "Int32":
                int valInt;
                Int32.TryParse(valString, out valInt);
                animator.SetInteger(param, valInt);
                break;

            default:
                print("Fuck you");
                break;

        }
    }
}

