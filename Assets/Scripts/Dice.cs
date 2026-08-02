using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Dice : NetworkBehaviour
{
    [SerializeField]
    private float tolerance = 0.99f;

    [SerializeField]
    private float sleepVelocityThreshold = 0.01f;

    [SerializeField]
    private float sleepAngularThreshold = 0.01f;

    [SerializeField]
    private float minSettleTime = 0.5f;

    private Rigidbody rb;
    private bool hasStoppedRolling = false;
    private bool hasThrowDelayFinished = false;
    private float belowThresholdTimer = 0f;
    private int diceIndex = -1;

    public int Result { get; private set; } = -1;
    public bool HasSettled => hasStoppedRolling;

    public event System.Action<Dice, int> OnSettled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;
        if (!hasThrowDelayFinished || hasStoppedRolling)
            return;

        bool belowThreshold =
            rb.linearVelocity.sqrMagnitude < sleepVelocityThreshold * sleepVelocityThreshold
            && rb.angularVelocity.sqrMagnitude < sleepAngularThreshold * sleepAngularThreshold;

        if (belowThreshold)
        {
            belowThresholdTimer += Time.fixedDeltaTime;

            if (belowThresholdTimer >= minSettleTime)
            {
                hasStoppedRolling = true;
                Result = GetSideUp();
                OnSettled?.Invoke(this, Result);
            }
        }
        else
        {
            belowThresholdTimer = 0f;
        }
    }

    private int GetSideUp()
    {
        Vector3[] sides = new Vector3[]
        {
            transform.up.normalized, // 1
            -transform.forward.normalized, // 2
            transform.right.normalized, // 3
            -transform.right.normalized, // 4
            transform.forward.normalized, // 5
            -transform.up.normalized, // 6
        };

        for (int i = 0; i < sides.Length; i++)
        {
            if (Vector3.Dot(sides[i], Vector3.up) > tolerance)
            {
                return i + 1;
            }
        }

        int bestIndex = 0;
        float bestDot = float.NegativeInfinity;
        for (int i = 0; i < sides.Length; i++)
        {
            float dot = Vector3.Dot(sides[i], Vector3.up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestIndex = 0;
            }
        }

        return bestIndex + 1;
    }

    internal void RollDice(float throwForce, float rollForce, int _diceIndex)
    {
        if (!IsServer)
            return;

        diceIndex = _diceIndex;
        hasStoppedRolling = false;
        hasThrowDelayFinished = false;
        belowThresholdTimer = 0f;
        Result = -1;

        float variance = Random.Range(-1f, 1f);

        rb.AddForce(transform.up * (throwForce + variance), ForceMode.Impulse);

        float rollX = Random.Range(0f, 1f);
        float rollY = Random.Range(0f, 1f);
        float rollZ = Random.Range(0f, 1f);
        Vector3 baseTorque = new(rollX, rollY, rollZ);

        rb.AddTorque(baseTorque * (rollForce + variance));

        StartCoroutine(ThrowDelay());
    }

    private IEnumerator ThrowDelay()
    {
        yield return new WaitForSeconds(1);
        hasThrowDelayFinished = true;
    }
}
