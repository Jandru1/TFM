/*
 * Circle.cs
 * RVO2 Library C#
 *
 * Copyright (c) 2008-2015 University of North Carolina at Chapel Hill.
 * All rights reserved.
 *
 * Permission to use, copy, modify, and distribute this software and its
 * documentation for educational, research, and non-profit purposes, without
 * fee, and without a written agreement is hereby granted, provided that the
 * above copyright notice, this paragraph, and the following four paragraphs
 * appear in all copies.
 *
 * Permission to incorporate this software into commercial products may be
 * obtained by contacting the authors <geom@cs.unc.edu> or the Office of
 * Technology Development at the University of North Carolina at Chapel Hill
 * <otd@unc.edu>.
 *
 * This software program and documentation are copyrighted by the University of
 * North Carolina at Chapel Hill. The software program and documentation are
 * supplied "as is," without any accompanying services from the University of
 * North Carolina at Chapel Hill or the authors. The University of North
 * Carolina at Chapel Hill and the authors do not warrant that the operation of
 * the program will be uninterrupted or error-free. The end-user understands
 * that the program was developed for research purposes and is advised not to
 * rely exclusively on the program for any reason.
 *
 * IN NO EVENT SHALL THE UNIVERSITY OF NORTH CAROLINA AT CHAPEL HILL OR THE
 * AUTHORS BE LIABLE TO ANY PARTY FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR
 * CONSEQUENTIAL DAMAGES, INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS
 * SOFTWARE AND ITS DOCUMENTATION, EVEN IF THE UNIVERSITY OF NORTH CAROLINA AT
 * CHAPEL HILL OR THE AUTHORS HAVE BEEN ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 *
 * THE UNIVERSITY OF NORTH CAROLINA AT CHAPEL HILL AND THE AUTHORS SPECIFICALLY
 * DISCLAIM ANY WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE AND ANY
 * STATUTORY WARRANTY OF NON-INFRINGEMENT. THE SOFTWARE PROVIDED HEREUNDER IS ON
 * AN "AS IS" BASIS, AND THE UNIVERSITY OF NORTH CAROLINA AT CHAPEL HILL AND THE
 * AUTHORS HAVE NO OBLIGATIONS TO PROVIDE MAINTENANCE, SUPPORT, UPDATES,
 * ENHANCEMENTS, OR MODIFICATIONS.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

/*
 * Example file showing a demo with 250 agents initially positioned evenly
 * distributed on a circle attempting to move to the antipodal position on the
 * circle.
 */

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

namespace RVO
{
	public class RVODemoCircle : MonoBehaviour
    {

		protected class SimpleAgent {

			public RVO.Vector2 _goal;

			protected int _id;
			protected GameObject _graphics; //crea el cilindro

			public SimpleAgent(int id) {
				_id = id;

				_graphics = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _graphics.AddComponent<Rigidbody>();
                _graphics.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
			}

			public virtual void Destroy() {
				DestroyImmediate(_graphics);
			}

			public virtual int GetID() {
				return _id;
			}

			public virtual void SetPreferredVelocity() {
				RVO.Vector2 goalVector = _goal - Simulator.Instance.getAgentPosition(_id);

				if (RVOMath.absSq(goalVector) > 1.0f) {
					goalVector = RVOMath.normalize(goalVector);
				}

				Simulator.Instance.setAgentPrefVelocity(_id, goalVector);
			}

			public virtual void Update() {
                Option1();
			}

            void Option1()
            {
                RVO.Vector2 pos = Simulator.Instance.getAgentPosition(_id);
                _graphics.transform.position = new Vector3(pos.x(), 0.0f, pos.y());
            }

            void Option2()
            {
                RVO.Vector2 v = Simulator.Instance.getAgentPrefVelocity(_id);
                _graphics.GetComponent<Rigidbody>().velocity = new Vector3(v.x(), 0.0f, v.y());
            }
		}

		#region PROTECTED PARAMETERS

		private List<SimpleAgent> agents = new List<SimpleAgent>();

		#endregion

		#region CREATION AND DESTRUCTION

		protected virtual void Awake() 
        {
            /* Specify the global time step of the simulation. */
            Simulator.Instance.setTimeStep(0.25f);
			Simulator.Instance.setAgentDefaults(15.0f, 10, 10.0f, 10.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

            for (int i = 0; i < 250; ++i) CreateAgent(i);
        }

		protected virtual void CreateAgent(int pos) {
			int agentID = Simulator.Instance.addAgent(200.0f * new Vector2((float)Math.Cos(pos * 2.0f * Math.PI / 250.0f), (float)Math.Sin(pos * 2.0f * Math.PI / 250.0f)));
            SimpleAgent agent = new SimpleAgent(agentID);
			agent._goal = -Simulator.Instance.getAgentPosition(agentID);
			agents.Add(agent);
		}

		protected virtual void DestroyAgent(SimpleAgent a) {
			Simulator.Instance.removeAgent(a.GetID());
			agents.Remove(a);
			a.Destroy();
		}

		#endregion

        protected virtual void Update() {
			if (Input.GetKeyDown(KeyCode.I)) {
				CreateAgent(0);
			}
			if (Input.GetKeyDown(KeyCode.R)) DestroyAgent(agents[0]);

			foreach (SimpleAgent a in agents) a.SetPreferredVelocity();
			Simulator.Instance.doStep();
			foreach (SimpleAgent a in agents) a.Update();
		}

    }
}
