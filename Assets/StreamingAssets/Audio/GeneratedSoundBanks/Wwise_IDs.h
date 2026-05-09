/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID PLAY_ECHOPULSE_START = 3841466509U;
        static const AkUniqueID PLAY_ECHOPULSE_STOP = 700059983U;
        static const AkUniqueID PLAY_ENEMY_DEATH_GOLEM = 3300476880U;
        static const AkUniqueID PLAY_ENEMY_DEATH_WISP = 4220059257U;
        static const AkUniqueID PLAY_ENEMY_HIT_GOLEM = 103692789U;
        static const AkUniqueID PLAY_ENEMY_HIT_WISP = 2982724738U;
        static const AkUniqueID PLAY_GATEDOOR_CLOSE = 4102059022U;
        static const AkUniqueID PLAY_GATEDOOR_MOVINGLOOP = 1378571526U;
        static const AkUniqueID PLAY_GATEDOOR_OPEN = 194403786U;
        static const AkUniqueID PLAY_GATEDOOR_STOP = 1896094386U;
        static const AkUniqueID PLAY_GOLEM_FOOTSTEP = 3759963795U;
        static const AkUniqueID PLAY_LAMP_DEPLETING = 3114152379U;
        static const AkUniqueID PLAY_LAMP_EMPTY = 3672837954U;
        static const AkUniqueID PLAY_LAMP_FILLING = 3244476280U;
        static const AkUniqueID PLAY_LAMP_FULL = 2703900524U;
        static const AkUniqueID PLAY_PLATFORM_STARTMOVE = 3318357497U;
        static const AkUniqueID PLAY_PLATFORM_STOPMOVE = 4046392273U;
        static const AkUniqueID PLAY_PLATFORM_TRAVELLOOP = 411136912U;
        static const AkUniqueID PLAY_RESONANCEDOOR_CLOSE = 3367557195U;
        static const AkUniqueID PLAY_RESONANCEDOOR_OPEN = 616110093U;
        static const AkUniqueID PLAY_RESONANCEHUM_DEPLETED = 4169607118U;
        static const AkUniqueID PLAY_RESONANCEHUM_START = 1953327225U;
        static const AkUniqueID PLAY_RESONANCEHUM_STOP = 2563595731U;
        static const AkUniqueID PLAYER_FOOTSTEP = 2453392179U;
        static const AkUniqueID PLAYER_LAND = 3629196698U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace LAMP_STATE
        {
            static const AkUniqueID GROUP = 177703081U;

            namespace STATE
            {
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace LAMP_STATE

        namespace PLATFORMMOVEMENT_STATE
        {
            static const AkUniqueID GROUP = 3250902065U;

            namespace STATE
            {
                static const AkUniqueID NONE = 748895195U;
            } // namespace STATE
        } // namespace PLATFORMMOVEMENT_STATE

    } // namespace STATES

    namespace SWITCHES
    {
        namespace SURFACETYPE
        {
            static const AkUniqueID GROUP = 63790334U;

            namespace SWITCH
            {
                static const AkUniqueID DEFAULT = 782826392U;
                static const AkUniqueID GRASS = 4248645337U;
                static const AkUniqueID LEAVES = 582824249U;
                static const AkUniqueID METAL = 2473969246U;
                static const AkUniqueID SOIL = 687380132U;
                static const AkUniqueID STONE = 1216965916U;
                static const AkUniqueID WOOD = 2058049674U;
            } // namespace SWITCH
        } // namespace SURFACETYPE

    } // namespace SWITCHES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID DOOR_OPENAMOUNT = 3849429492U;
        static const AkUniqueID ECHOPULSE_FREQUENCY = 3773845682U;
        static const AkUniqueID LAMP_ENERGY = 2086212122U;
        static const AkUniqueID PLAYER_SPEED = 1062779386U;
        static const AkUniqueID RESONANCEHUM_ENERGY = 3818301486U;
        static const AkUniqueID RESONANCEHUM_INTENSITY = 1925913377U;
    } // namespace GAME_PARAMETERS

    namespace BANKS
    {
        static const AkUniqueID INIT = 1355168291U;
        static const AkUniqueID ENEMIES = 2242381963U;
        static const AkUniqueID ENVIRONMENT_BANK = 430783581U;
        static const AkUniqueID PLAYER_SFX = 817096458U;
    } // namespace BANKS

    namespace BUSSES
    {
        static const AkUniqueID BUS_DOORS = 153992835U;
        static const AkUniqueID BUS_LAMP = 677043558U;
        static const AkUniqueID BUS_PLATFORM = 130819113U;
        static const AkUniqueID ENVIRONMENT_BUS = 1523166483U;
        static const AkUniqueID MAIN_AUDIO_BUS = 2246998526U;
        static const AkUniqueID PLAYER_BUS = 1138681361U;
    } // namespace BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
