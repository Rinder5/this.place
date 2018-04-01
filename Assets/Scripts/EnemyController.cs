﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public GameObject EyesPivot;
    public LayerMask BlockLayer;
    private BlockFaceBehaviour _inner;
    private Vector3 _newPosition;

    private bool _isMoving;
    // Use this for initialization
    void Awake()
    {
        _inner = GetComponentInChildren<BlockFaceBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isMoving)
        {
            if (Vector3.Distance(transform.position, _newPosition) <= 0.01f)
            {
                transform.position = _newPosition;
                _isMoving = false;
            }
            else
            {
                transform.Translate((_newPosition - transform.position).normalized * Time.deltaTime);

            }
        }
    }

    public void MoveEnemy(BlockFace face)
    {
        if (!_inner.FireRaycastFromFace(0.1f, BlockLayer, face))
        {
            _newPosition = transform.position + face.GetNormal();
            _isMoving = true;
        }
    }

    public void LookAtFaceDir(BlockFace face)
    {
        EyesPivot.transform.forward = face.GetNormal();
    }

    public bool IsMoving()
    {
        return _isMoving;
    }

    // we handle the box collision here.  we should have a separate component
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GetComponentInChildren<EnemyAI>().EnemyDeath();
        }
    }
}
