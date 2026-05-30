/// <summary>
/// UI·토스트·경고 팝업에 쓰는 메시지 식별자.
/// 표시 문구는 <see cref="MessageCatalog"/> 에서 key 로 조회합니다.
/// </summary>
public static class MessageKeys
{
    // Login
    public const string LoginEmptyCredentials = "login.empty_credentials";
    public const string LoginInvalidCredentials = "login.invalid_credentials";
    public const string LoginNetworkFailure = "login.network_failure";

    // Network
    public const string ServerDisconnected = "network.server_disconnected";
    public const string ConnectFailed = "network.connect_failed";
    public const string NotConnected = "network.not_connected";
    public const string LoginInProgress = "network.login_in_progress";
    public const string EnterGameFailed = "network.enter_game_failed";
    public const string ProtocolUnreadable = "network.protocol_unreadable";

    // Lobby
    public const string InviteFailed = "lobby.invite_failed";
    public const string InviteFailedWithReason = "lobby.invite_failed_with_reason";
    public const string InviteResponseFailed = "lobby.invite_response_failed";
    public const string InviteResponseFailedWithReason = "lobby.invite_response_failed_with_reason";
    public const string InviteTargetNameRequired = "lobby.invite_target_name_required";
    public const string InviteTargetTagInvalid = "lobby.invite_target_tag_invalid";
    public const string InviteSelfNotAllowed = "lobby.invite_self_not_allowed";
    public const string InviteNotification = "lobby.invite_notification";
    public const string MultiplayLoginRequired = "lobby.multiplay_login_required";
    public const string CreateRoomFailedLeavePrevious = "lobby.create_room_failed_leave_previous";
    public const string CreateRoomFailed = "lobby.create_room_failed";
    public const string CreateRoomFailedWithReason = "lobby.create_room_failed_with_reason";
    public const string EnterRoomFailed = "lobby.enter_room_failed";
    public const string EnterRoomFailedWithReason = "lobby.enter_room_failed_with_reason";
    public const string StartRoomFailed = "lobby.start_room_failed";
    public const string StartRoomFailedWithReason = "lobby.start_room_failed_with_reason";
    public const string LeaveRoomFailed = "lobby.leave_room_failed";

    // Stage
    public const string StartStageRejected = "stage.start_rejected";
    public const string ClearInfoFailed = "stage.clear_info_failed";
    public const string GameClearFailed = "stage.game_clear_failed";

    // Gacha
    public const string GachaRequestPending = "gacha.request_pending";
    public const string GachaSpinInProgress = "gacha.spin_in_progress";
    public const string GachaInvalidPool = "gacha.invalid_pool";
    public const string GachaFailed = "gacha.failed";
    public const string GachaFailedWithReason = "gacha.failed_with_reason";
    public const string GachaNoResult = "gacha.no_result";
    public const string GachaSpinStartFailed = "gacha.spin_start_failed";

    // Craft / gameplay
    public const string FurnaceBusyOrHasResult = "craft.furnace_busy_or_has_result";
    public const string FurnaceStillWorking = "craft.furnace_still_working";
    public const string FurnaceCannotSmelt = "craft.furnace_cannot_smelt";
    public const string FurnaceStillProcessing = "craft.furnace_still_processing";
    public const string SpaceshipNotItem = "craft.spaceship_not_item";
    public const string SpaceshipNoItemComponent = "craft.spaceship_no_item_component";
    public const string SpaceshipWrongItem = "craft.spaceship_wrong_item";
    public const string CraftTableEmpty = "craft.craft_table_empty";
    public const string CraftPrefabNotFound = "craft.craft_prefab_not_found";
    public const string FurnaceAlreadySmeltedItem = "craft.furnace_already_smelted_item";
    public const string FurnaceLegacyBusy = "craft.furnace_legacy_busy";
    public const string FurnaceLegacyNotItem = "craft.furnace_legacy_not_item";
    public const string FurnaceLegacyNoRecipe = "craft.furnace_legacy_no_recipe";

    // Key binding
    public const string KeyBindingConflictChoice = "keybinding.conflict_choice";
}
