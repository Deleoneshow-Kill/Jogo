using UnityEngine;

namespace CleanRPG.Systems
{
    /// <summary>
    /// Componente simples para rotacionar objetos continuamente
    /// </summary>
    public class Rotator : MonoBehaviour
    {
        [SerializeField] public float speed = 10f;
        [SerializeField] public Vector3 axis = Vector3.up;
        
        void Update()
        {
            transform.Rotate(axis, speed * Time.deltaTime);
        }
    }
}