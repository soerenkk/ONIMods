﻿using Harmony;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Blueprints {
    public sealed class SnapshotTool : MultiFilteredDragTool {
        private Blueprint blueprint = null;

        public static SnapshotTool Instance { get; private set; }

        public SnapshotTool() {
            Instance = this;
        }

        public static void DestroyInstance() {
            Instance = null;
        }

        public void CreateVisualizer() {
            if (visualizer != null) {
                Destroy(visualizer);
            }

            visualizer = new GameObject("SnapshotVisualizer");
            visualizer.SetActive(false);

            GameObject offsetObject = new GameObject();
            SpriteRenderer spriteRenderer = offsetObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = BlueprintsAssets.BLUEPRINTS_COLOR_BLUEPRINT_DRAG;
            spriteRenderer.sprite = BlueprintsAssets.BLUEPRINTS_SNAPSHOT_VISUALIZER_SPRITE;

            offsetObject.transform.SetParent(visualizer.transform);
            offsetObject.transform.localPosition = new Vector3(0, Grid.HalfCellSizeInMeters);
            offsetObject.transform.localScale = new Vector3(
                Grid.CellSizeInMeters / (spriteRenderer.sprite.texture.width / spriteRenderer.sprite.pixelsPerUnit),
                Grid.CellSizeInMeters / (spriteRenderer.sprite.texture.height / spriteRenderer.sprite.pixelsPerUnit)
            );

            offsetObject.SetLayerRecursively(LayerMask.NameToLayer("Overlay"));
            visualizer.transform.SetParent(transform);

            OnMouseMove(PlayerController.GetCursorPos(KInputManager.GetMousePos()));
        }

        public void DestroyVisualizer() {
            Destroy(visualizer);
            visualizer = null;
        }

        public void DeleteBlueprint() {
            blueprint = null;

            gameObject.GetComponent<SnapshotToolHoverCard>().UsingSnapshot = false;

            MultiToolParameterMenu.Instance.PopulateMenu(GetDefaultFilters());
            MultiToolParameterMenu.Instance.ShowMenu();
            ToolMenu.Instance.PriorityScreen.Show(false);
            BlueprintsState.ClearVisuals();

            CreateVisualizer();
        }

        protected override void OnPrefabInit() {
            base.OnPrefabInit();

            FieldInfo areaVisualizerField = AccessTools.Field(typeof(DragTool), "areaVisualizer");
            FieldInfo areaVisualizerSpriteRendererField = AccessTools.Field(typeof(DragTool), "areaVisualizerSpriteRenderer");

            GameObject areaVisualizer = Util.KInstantiate(Traverse.Create(DeconstructTool.Instance).Field("areaVisualizer").GetValue<GameObject>());
            areaVisualizer.SetActive(false);

            areaVisualizer.name = "SnapshotAreaVisualizer";
            areaVisualizerSpriteRendererField.SetValue(this, areaVisualizer.GetComponent<SpriteRenderer>());
            areaVisualizer.transform.SetParent(transform);
            areaVisualizer.GetComponent<SpriteRenderer>().color = BlueprintsAssets.BLUEPRINTS_COLOR_BLUEPRINT_DRAG;
            areaVisualizer.GetComponent<SpriteRenderer>().material.color = BlueprintsAssets.BLUEPRINTS_COLOR_BLUEPRINT_DRAG;

            areaVisualizerField.SetValue(this, areaVisualizer);

            gameObject.AddComponent<SnapshotToolHoverCard>();
        }

        protected override void OnActivateTool() {
            base.OnActivateTool();

            if (visualizer == null) {
                CreateVisualizer();
            }

            gameObject.GetComponent<SnapshotToolHoverCard>().UsingSnapshot = false;
        }

        protected override void OnDeactivateTool(InterfaceTool newTool) {
            base.OnDeactivateTool(newTool);

            BlueprintsState.ClearVisuals();
            blueprint = null;

            MultiToolParameterMenu.Instance.HideMenu();
            ToolMenu.Instance.PriorityScreen.Show(false);
            GridCompositor.Instance.ToggleMajor(false);
        }

        protected override void OnDragComplete(Vector3 cursorDown, Vector3 cursorUp) {
            base.OnDragComplete(cursorDown, cursorUp);

            if (hasFocus) {
                Grid.PosToXY(cursorDown, out int x0, out int y0);
                Grid.PosToXY(cursorUp, out int x1, out int y1);

                if (x0 > x1) {
                    Util.Swap(ref x0, ref x1);
                }

                if (y0 < y1) {
                    Util.Swap(ref y0, ref y1);
                }

                Blueprint blueprint = BlueprintsState.CreateBlueprint(new Vector2I(x0, y0), new Vector2I(x1, y1), MultiToolParameterMenu.Instance);
                if (blueprint.IsEmpty()) {
                    PopFXManager.Instance.SpawnFX(BlueprintsAssets.BLUEPRINTS_CREATE_ICON_SPRITE, Strings.Get(BlueprintsStrings.STRING_BLUEPRINTS_SNAPSHOT_EMPTY), null, PlayerController.GetCursorPos(KInputManager.GetMousePos()), BlueprintsAssets.Options.FXTime);
                }

                else {
                    BlueprintsState.VisualizeBlueprint(Grid.PosToXY(PlayerController.GetCursorPos(KInputManager.GetMousePos())), blueprint);

                    MultiToolParameterMenu.Instance.HideMenu();
                    ToolMenu.Instance.PriorityScreen.Show(true);

                    gameObject.GetComponent<SnapshotToolHoverCard>().UsingSnapshot = true;
                    DestroyVisualizer();

                    PopFXManager.Instance.SpawnFX(BlueprintsAssets.BLUEPRINTS_CREATE_ICON_SPRITE, Strings.Get(BlueprintsStrings.STRING_BLUEPRINTS_SNAPSHOT_TAKEN), null, PlayerController.GetCursorPos(KInputManager.GetMousePos()), BlueprintsAssets.Options.FXTime);
                    GridCompositor.Instance.ToggleMajor(true);
                    this.blueprint = blueprint;
                }
            }
        }

        public override void OnLeftClickDown(Vector3 cursorPos) {
            if (blueprint == null) {
                base.OnLeftClickDown(cursorPos);
            }

            else if (hasFocus) {
                BlueprintsState.UseBlueprint(Grid.PosToXY(cursorPos));
            }
        }

        public override void OnLeftClickUp(Vector3 cursorPos) {
            if (blueprint == null) {
                base.OnLeftClickUp(cursorPos);
            }
        }

        public override void OnMouseMove(Vector3 cursorPos) {
            if (blueprint == null) {
                base.OnMouseMove(cursorPos);
            }

            else if (hasFocus) {
                BlueprintsState.UpdateVisual(Grid.PosToXY(cursorPos));
            }
        }

        public override void OnKeyDown(KButtonEvent buttonEvent) {
            if (buttonEvent.TryConsume(BlueprintsAssets.BLUEPRINTS_MULTI_DELETE.GetKAction())) {
                Instance.DeleteBlueprint();
                GridCompositor.Instance.ToggleMajor(false);
            }

            base.OnKeyDown(buttonEvent);
        }

        protected override Dictionary<string, ToolParameterMenu.ToggleState> GetDefaultFilters() {
            return new Dictionary<string, ToolParameterMenu.ToggleState> {
                { ToolParameterMenu.FILTERLAYERS.WIRES, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.LIQUIDCONDUIT, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.GASCONDUIT, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.SOLIDCONDUIT, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.BUILDINGS, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.LOGIC, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.BACKWALL, ToolParameterMenu.ToggleState.On },
                { ToolParameterMenu.FILTERLAYERS.DIGPLACER, ToolParameterMenu.ToggleState.On },
                { BlueprintsStrings.STRING_BLUEPRINTS_MULTIFILTER_GASTILES, ToolParameterMenu.ToggleState.On },
            };
        }
    }
}
