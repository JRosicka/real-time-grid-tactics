using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for moving a <see cref="GridEntity"/>
    /// </summary>
    public class MoveAbility : AbilityBase<MoveAbilityData, MoveAbilityParameters> {
        private MoveAbilityParameters AbilityParameters => (MoveAbilityParameters) BaseParameters;
        private int _moveCost;
        
        public MoveAbility(MoveAbilityData data, MoveAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        public override void CompleteCooldown() {
            Performer.CurrentMoves = Mathf.Min(Performer.CurrentMoves + _moveCost, Performer.MaxMove);
        }

        protected override void PayCost() {
            _moveCost = GameManager.Instance.PathfinderService.RequiredMoves(Performer, Performer.Location,
                    AbilityParameters.Destination);
            if (_moveCost > Performer.CurrentMoves) {
                Debug.LogError($"Tried to pay too high of a move cost ({_moveCost}) for entity {Performer.DisplayName} with move amount ({Performer.CurrentMoves})");
                _moveCost = Performer.CurrentMoves;
            }

            Performer.CurrentMoves -= _moveCost;
            base.PayCost();
        }
        
        public override void DoPerformAbility() {
            Debug.Log($"Did move ability to {AbilityParameters.Destination}, cool");
            GameManager.Instance.CommandManager.MoveEntityToCell(Performer, AbilityParameters.Destination);
        }
    }

    public class MoveAbilityParameters : IAbilityParameters {
        public Vector2Int Destination;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Destination);
        }

        public void Deserialize(NetworkReader reader) {
            Destination = reader.Read<Vector2Int>();
        }
    }
}