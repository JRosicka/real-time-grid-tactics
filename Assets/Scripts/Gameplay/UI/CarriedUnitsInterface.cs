using System;
using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Central UI for <see cref="CarriedUnitSlot"/>s in the <see cref="SelectionInterface"/>
    /// </summary>
    public class CarriedUnitsInterface : MonoBehaviour {
        [SerializeField] private CarriedUnitSlot _emptySlotPrefab;
        [SerializeField] private CarriedUnitSlot _normalSlotPrefab;
        [SerializeField] private CarriedUnitSlot _largeSlotPrefab;
        
        private GridEntity _carrier;
        private readonly List<CarriedUnitSlot> _activeSlots = new List<CarriedUnitSlot>();

        public void Initialize() {
            _emptySlotPrefab.gameObject.SetActive(false);
            _normalSlotPrefab.gameObject.SetActive(false);
            _largeSlotPrefab.gameObject.SetActive(false);
        }
        
        public void SetUpForEntity(GridEntity carrier) {
            _carrier = carrier;
            DropOffUnitAbilityData dropOffData = carrier.GetAbilityData<DropOffUnitAbilityData>();
            if (dropOffData == null) {
                HideInterface();
                return;
            }

            if (carrier.CarryCapacity == 0) {
                HideInterface();
                return;
            }
            
            gameObject.SetActive(true);
            ClearSlots();
            carrier.CarriedEntitiesField.ValueChanged += CarriedEntitiesUpdated;
            UpdateSlots();
        }

        public void HideInterface() {
            gameObject.SetActive(false);
            ClearSlots();
            if (_carrier) {
                _carrier.CarriedEntitiesField.ValueChanged -= CarriedEntitiesUpdated;
                _carrier = null;
            }
        }

        public void SelectUnitToDropOff(GridEntity entity) {
            if (entity == null) return;
            
            DropOffUnitAbilityData dropOffData = _carrier.GetAbilityData<DropOffUnitAbilityData>();
            dropOffData?.SelectAbility(_carrier, entity);
        }

        private void UpdateSlots() {
            int carryCapacity = _carrier.CarryCapacity;
            switch (carryCapacity) {
                case 0:
                    return;
                case 1:
                case 2:
                    int filledCapacity = 0;
                    foreach (GridEntity entity in _carrier.CarriedEntities) {
                        if (entity.EntityData.Tags.Contains(EntityTag.Massive)) {
                            // Use a large slot
                            SetUpSlot(_largeSlotPrefab, entity);
                            filledCapacity += 2;
                        } else {
                            SetUpSlot(_normalSlotPrefab, entity);
                            filledCapacity++;
                        }
                    }
                    
                    // Fill up empty slots for remainder
                    for (int i = filledCapacity; i < carryCapacity; i++) {
                        SetUpSlot(_emptySlotPrefab, null);
                    }
                    return;
                default:
                    throw new Exception($"Carry capacity of {carryCapacity} not supported in UI");
            }
        }

        private void SetUpSlot(CarriedUnitSlot slotPrefab, GridEntity entityForSlot) {
            CarriedUnitSlot slot = Instantiate(slotPrefab, transform);
            slot.gameObject.SetActive(true);
            slot.Initialize(this, entityForSlot);
            _activeSlots.Add(slot);
        }

        private void ClearSlots() {
            foreach (CarriedUnitSlot carriedUnitSlot in _activeSlots) {
                Destroy(carriedUnitSlot.gameObject);
            }
            _activeSlots.Clear();
        }

        private void CarriedEntitiesUpdated(INetworkableFieldValue oldValue, INetworkableFieldValue newValue, string metadata) {
            ClearSlots();
            UpdateSlots();
        }
    }
}