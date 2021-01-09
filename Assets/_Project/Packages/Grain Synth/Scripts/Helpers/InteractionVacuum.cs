using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionVacuum : InteractionBase
{
    public enum InteractionVacuumType
    {
        SuckAmount,
        BlowAmount,
        VacuumMass,
    }

    private Instrument_Vacuum _VacuumInstrument;

    public InteractionVacuumType _SourceParameter;

    [Range(0f, 1f)]
    public float _Smoothing = 0.2f;

    private void Start()
    {
        _VacuumInstrument = _SourceObject.GetComponent<Instrument_Vacuum>();
    }

    void Update()
    {
        float currentValue = _PreviousInputValue;

        if (_VacuumInstrument != null)
        {
            switch (_SourceParameter)
            {
                case InteractionVacuumType.SuckAmount:
                    currentValue = _VacuumInstrument._ThumbScalar;
                    break;
                case InteractionVacuumType.BlowAmount:
                    currentValue = _VacuumInstrument._ThumbScalar;
                    break;
                case InteractionVacuumType.VacuumMass:
                    currentValue = _VacuumInstrument._TotalVacuumedMass;
                    break;
                default:
                    break;
            }
        }

        UpdateSmoothedOutputValue(currentValue, _Smoothing);
    }
}
