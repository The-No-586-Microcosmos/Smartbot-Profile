using System;
using System.Collections.Generic;
using System.Linq;
using SmartBot.Database;
using SmartBot.Plugins.API;

namespace SmartBotAPI.Plugins.API
{

    public class SMK_WildSecretMage : Archetype
    {
        public string ArchetypeName()
        {
            return "SMK_Wild_Secret_Mage";
        }

        public List<Card.Cards> ArchetypeCardSet()
        {
            return new List<Card.Cards>()
            {
                Cards.KabalLackey,				//������̴�
                Cards.MedivhsValet,				//����ĵ�����
                Cards.AncientMysteries,			//Զ������
                Cards.Arcanologist,				//�ط�ѧ��
                Cards.MadScientist,				//���Ŀ�ѧ��
                Cards.ArcaneFlakmage,			//�Կհ�����ʦ
                Cards.MirrorEntity,				//����ʵ��
                Cards.KirinTorMage,				//�����з�ʦ
                Cards.ForgottenTorch,			//�ϾɵĻ��
                Cards.ExplosiveRunes,			//��ը����
                Cards.FlameWard,				//������
                Cards.Counterspell,				//��������
                Cards.IceBlock,					//��������
                Cards.PolymorphBoar,			//��������Ұ��
                Cards.Fireball,					//������
                Cards.CloudPrince,				//��������
                Cards.Aluneth,					//��¶��˹
                Cards.KabalCrystalRunner,		//�����ˮ����Ů

                //����
                Cards.Secretkeeper,				//�����ػ���
				Cards.StargazerLuna,            //������¶��
                Cards.Duplicate,				//����
                Cards.PotionofPolymorph,		//����ҩˮ
                Cards.ManaBind,					//��������
                Cards.Vaporize,					//����
                Cards.Spellbender,				//����
                Cards.SplittingImage,			//�ѻ����
                Cards.Effigy					//�ֻ�
            };
        }
    }
}