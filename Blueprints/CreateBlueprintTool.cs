﻿using Harmony;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Blueprints {
    public sealed class CreateBlueprintTool : MultiFilteredDragTool {
        public static CreateBlueprintTool Instance { get; private set; }

        public CreateBlueprintTool() {
            Instance = this;
        }

        public static void DestroyInstance() {
            Instance = null;
        }

        protected override void OnPrefabInit() {
            base.OnPrefabInit();

            visualizer = new GameObject("CreateBlueprintVisualizer");
            visualizer.SetActive(false);

            GameObject offsetObject = new GameObject();
            SpriteRenderer spriteRenderer = offsetObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = BlueprintsAssets.BLUEPRINTS_COLOR_BLUEPRINT_DRAG;
            spriteRenderer.sprite = BlueprintsAssets.BLUEPRINTS_CREATE_VISUALIZER_SPRITE;

            offsetObject.transform.SetParent(visualizer.transform);
            offsetObject.transform.localPosition = new Vector3(0, Grid.HalfCellSizeInMeters);
            offsetObject.transform.localScale = new Vector3(
                Grid.CellSizeInMeters / (spriteRenderer.sprite.texture.width / spriteRenderer.sprite.pixelsPerUnit),
                Grid.CellSizeInMeters / (spriteRenderer.sprite.texture.height / spriteRenderer.sprite.pixelsPerUnit)
            );

            offsetObject.SetLayerRecursively(LayerMask.NameToLayer("Overlay"));
            visualizer.transform.SetParent(transform);

            FieldInfo areaVisualizerField = AccessTools.Field(typeof(DragTool), "areaVisualizer");
            FieldInfo areaVisualizerSpriteRendererField = AccessTools.Field(typeof(DragTool), "areaVisualizerSpriteRenderer");

            GameObject areaVisualizer = Util.KInstantiate(Traverse.Create(DeconstructTool.Instance).Field("areaVisualizer").GetValue<GameObject>());
            areaVisualizer.SetActive(false);

            areaVisualizer.name = "CreateBlueprintAreaVisualizer";
            areaVisualizerSpriteRendererField.SetValue(this, areaVisualizer.GetComponent<SpriteRenderer>());
            areaVisualizer.transform.SetParent(transform);
            areaVisualizer.GetComponent<SpriteRenderer>().color = BlueprintsAssets.BLUEPRINTS_COLOR_BLUEPRINT_DRAG;
            areaVisualizer.GetComponent<SpriteRenderer>().material.color = BlueprintsAssets.BLUEPRINTS_COLOR_BLUEPRINT_DRAG;

            areaVisualizerField.SetValue(this, areaVisualizer);

            gameObject.AddComponent<CreateBlueprintToolHoverCard>();
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
                    PopFXManager.Instance.SpawnFX(BlueprintsAssets.BLUEPRINTS_CREATE_ICON_SPRITE, Strings.Get(BlueprintsStrings.STRING_BLUEPRINTS_CREATE_EMPTY), null, PlayerController.GetCursorPos(KInputManager.GetMousePos()), BlueprintsAssets.Options.FXTime);
                }

                else {
                    void onConfirmDelegate(string blueprintName, FileNameDialog parent) {
                        blueprint.Rename(blueprintName, false);
                        blueprint.SetFolder("");

                        SpeedControlScreen.Instance.Unpause(false);

                        PopFXManager.Instance.SpawnFX(BlueprintsAssets.BLUEPRINTS_CREATE_ICON_SPRITE, Strings.Get(BlueprintsStrings.STRING_BLUEPRINTS_CREATE_CREATED), null, PlayerController.GetCursorPos(KInputManager.GetMousePos()), BlueprintsAssets.Options.FXTime);
                        parent.Deactivate();
                    }

                    FileNameDialog blueprintNameDialog = UIUtilities.CreateTextDialog(Strings.Get(BlueprintsStrings.STRING_BLUEPRINTS_NAMEBLUEPRINT_TITLE), false, onConfirmDelegate);
                    SpeedControlScreen.Instance.Pause(false);

                    blueprintNameDialog.onCancel = delegate {
                        SpeedControlScreen.Instance.Unpause(false);

                        PopFXManager.Instance.SpawnFX(BlueprintsAssets.BLUEPRINTS_CREATE_ICON_SPRITE, Strings.Get(BlueprintsStrings.STRING_BLUEPRINTS_CREATE_CANCELLED), null, PlayerController.GetCursorPos(KInputManager.GetMousePos()), BlueprintsAssets.Options.FXTime);
                        blueprintNameDialog.Deactivate();
                    };

                    blueprintNameDialog.Activate();
                }
            }
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
