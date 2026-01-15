+ First I tried to use `ctx.ConsumerType`, but it doesn't actually make any
sense because there isn't a consumer type. This became apparent when inside
`RootBind` `ConsumerType` was `Composition`. Can't work inside normal `Bind`
because you can only resolve roots.

+ Next, I tried to use `Tags` because I noticed that you can resolve using a
`Tag` and define a `Tags.Any` on a binding that would match anything. When
using `RootBind` the `ctx.Tag` simply resolves to `Tag.Any` inside `To` and
this resulted in compilation issues as well (on example seemed like a bug in
the source code generation  `"ctx.Tag as Type" -> global::Pure.DI.Tag.Anyas
Type`). I guess this only works for the regular `Bind` as shown in [this
example](https://github.com/DevTeam/Pure.DI/blob/master/readme/tag-any.md) for
the `Queue`. (I figure this wouldn't work at runtime either even if you could
resolve non-roots because it's all resolved at compile time).

+ Next, I figured I could use the builder pattern; however, I still can't
resolve the handlers from the object graph because I can't figure out any way
to know which one I would actually have to make, so instead, I can create the
handler using a parameterless constructor, and then use the `Builder` method to
initialize all the dependencies.
  + I don't particularly like this design though because of an object being in
an invalid state until you call a particular method.
