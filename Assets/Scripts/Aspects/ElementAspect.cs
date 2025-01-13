﻿using Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Aspects
{
    public readonly partial struct ElementAspect : IAspect
    {
        private readonly Entity _entity;

        private readonly RefRO<LocalToWorld> _localToWorld;
        private readonly RefRW<PhysicsVelocity> _physicsVelocity;
        private readonly RefRW<TargetGravityComponent> _targetGravityComponent;
        private readonly RefRO<IndexConnectComponent> _indexConnect;

        private readonly EnabledRefRW<TargetGravityComponent> _enabledTargetGravityComponent;

        public bool EnableTargetGravity
        {
            set => _enabledTargetGravityComponent.ValueRW = value;
            get => _enabledTargetGravityComponent.ValueRO;
        }

        public TargetGravityComponent TargetGravity
        {
            set => _targetGravityComponent.ValueRW = value;
            get => _targetGravityComponent.ValueRO;
        }

        public float3 ToTargetGravity =>
            _targetGravityComponent.ValueRO.target != Entity.Null 
                ? math.normalizesafe(_targetGravityComponent.ValueRO.position - _localToWorld.ValueRO.Position) 
                : float3.zero;

        public float3 LinearVelocity
        {
            set => _physicsVelocity.ValueRW.Linear = value;
            get => _physicsVelocity.ValueRO.Linear;
        }

        public float DistanceToTargetGravity =>
            math.distance(_localToWorld.ValueRO.Position, _targetGravityComponent.ValueRO.position);

        public byte IndexConnect => _indexConnect.ValueRO.value;
    }
}