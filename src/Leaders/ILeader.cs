// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Enums;
using CivOne.Graphics;

namespace CivOne.Leaders
{
	public interface ILeader
	{
		string Name { get; set; }
		Picture GetPortrait(FaceState state = FaceState.Neutral);
		Picture PortraitSmall { get; }
		AggressionLevel Aggression { get; set; }
		DevelopmentLevel Development { get; set; }
		MilitarismLevel Militarism { get; set; }
	}
}