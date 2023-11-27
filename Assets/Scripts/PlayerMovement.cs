using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 2.0F;
    public float rotateSpeed = 1.0F;

    Rigidbody rb;
    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }
    void Update()
    {
        //print("я рожден ходить");
        CharacterController controller = GetComponent<CharacterController>();

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        float curSpeed = speed * Input.GetAxis("Vertical");

        Vector3 right = transform.TransformDirection(Vector3.right);
        float curSpeedHor = speed * Input.GetAxis("Horizontal");
        controller.SimpleMove(forward * curSpeed+ right * curSpeedHor);

        //print("Input.GetAxis(Vertical)"+ Input.GetAxis("Vertical"));
        //print("Input.GetAxis(Horizontal)" + Input.GetAxis("Horizontal"));


    }
}