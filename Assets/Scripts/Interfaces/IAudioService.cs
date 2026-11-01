using UnityEngine;

namespace AbandonedAsylum.Interfaces
{
    /// <summary>
    /// Contract cho Service phát âm thanh hiệu ứng (SFX) và nhạc nền toàn cục.
    /// Giúp phân tách lỏng (Decoupling) giữa các module gameplay và hệ thống Audio.
    /// </summary>
    public interface IAudioService
    {
        void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f);
        void PlayButtonClickSFX();
        void PlayButtonHoverSFX();
        void PlayClawSwipe(Vector3 pos);
        void PlayClawHit(Vector3 pos);
        void PlayBulletFleshHit(Vector3 pos);
        void PlayBulletPortalHit(Vector3 pos);
        void PlayPortalCollapse(Vector3 pos);
        void PlayBulletConcreteHit(Vector3 pos);
        void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f, float minDistance = 2.0f, float maxDistance = 30.0f, float spatialBlend = 1.0f);
        void PlayRifleShoot(Vector3 pos, AudioSource directSource = null);
        void PlayRifleReloadMagOut(Vector3 pos, AudioSource directSource = null);
        void PlayRifleReloadMagIn(Vector3 pos, AudioSource directSource = null);
        void PlayRifleReloadCharge(Vector3 pos, AudioSource directSource = null);
        void PlayZombieGrowl(AudioSource source);
        void PlayZombieAttack(AudioSource source);
        void PlayZombieHurt(AudioSource source);
        void PlayZombieDeath(AudioSource source);
    }
}
