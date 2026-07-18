using snap_test.Models;

namespace snap_test.Data
{
    /// <summary>
    /// In-memory seed store for recipes (15 records) across cuisines:
    /// Italian, Thai, Japanese, Mexican, Indian, American.
    /// </summary>
    public static class RecipeStore
    {
        public static List<Recipe> Recipes = new()
        {
            new Recipe { Id = 1, Title = "Spicy Thai Basil Chicken", Cuisine = "Thai", PrepTime = 15, CookTime = 10, Servings = 4, Difficulty = "medium", Rating = 4.7, Image = "https://picsum.photos/seed/recipe1/600/400", Tags = new() { "spicy", "quick", "asian" },
                Ingredients = new() { "500g chicken breast", "1 cup thai basil leaves", "4 cloves garlic", "3 thai chilies", "2 tbsp soy sauce", "1 tbsp fish sauce", "1 tbsp oyster sauce" },
                Instructions = new() { "Dice the chicken into small cubes.", "Mince garlic and slice chilies.", "Heat oil in a wok over high heat.", "Stir-fry garlic and chilies for 30 seconds.", "Add chicken, cook until no longer pink.", "Add sauces, toss to coat.", "Remove from heat, fold in basil leaves." } },
            new Recipe { Id = 2, Title = "Classic Margherita Pizza", Cuisine = "Italian", PrepTime = 20, CookTime = 12, Servings = 2, Difficulty = "easy", Rating = 4.8, Image = "https://picsum.photos/seed/recipe2/600/400", Tags = new() { "vegetarian", "classic", "cheese" },
                Ingredients = new() { "1 pizza dough ball", "1/2 cup tomato sauce", "200g fresh mozzarella", "Fresh basil leaves", "2 tbsp olive oil", "Salt to taste" },
                Instructions = new() { "Preheat oven to 250C with a pizza stone.", "Stretch the dough into a round base.", "Spread tomato sauce evenly.", "Tear mozzarella and distribute over sauce.", "Bake for 10-12 minutes until crust is golden.", "Top with fresh basil and a drizzle of olive oil." } },
            new Recipe { Id = 3, Title = "Chicken Teriyaki Bowl", Cuisine = "Japanese", PrepTime = 15, CookTime = 15, Servings = 2, Difficulty = "easy", Rating = 4.5, Image = "https://picsum.photos/seed/recipe3/600/400", Tags = new() { "rice", "quick", "asian" },
                Ingredients = new() { "2 chicken thighs", "3 tbsp soy sauce", "2 tbsp mirin", "1 tbsp sugar", "2 cups cooked rice", "1 spring onion", "Sesame seeds" },
                Instructions = new() { "Mix soy sauce, mirin, and sugar for the teriyaki sauce.", "Pan-fry chicken thighs skin-side down until crisp.", "Flip and cook through.", "Pour in the sauce and simmer until glossy.", "Slice chicken and serve over rice.", "Garnish with spring onion and sesame seeds." } },
            new Recipe { Id = 4, Title = "Beef Tacos al Pastor", Cuisine = "Mexican", PrepTime = 30, CookTime = 20, Servings = 4, Difficulty = "medium", Rating = 4.6, Image = "https://picsum.photos/seed/recipe4/600/400", Tags = new() { "spicy", "street-food", "grill" },
                Ingredients = new() { "600g beef steak", "3 dried guajillo chilies", "1/2 pineapple", "2 cloves garlic", "8 corn tortillas", "1 onion", "Fresh cilantro" },
                Instructions = new() { "Blend chilies, garlic, and spices into a marinade.", "Coat beef and marinate for 20 minutes.", "Grill the beef until charred, then dice.", "Char pineapple slices and chop.", "Warm the tortillas.", "Assemble tacos with beef, pineapple, onion, and cilantro." } },
            new Recipe { Id = 5, Title = "Butter Chicken", Cuisine = "Indian", PrepTime = 25, CookTime = 30, Servings = 4, Difficulty = "medium", Rating = 4.9, Image = "https://picsum.photos/seed/recipe5/600/400", Tags = new() { "creamy", "curry", "comfort" },
                Ingredients = new() { "700g chicken thigh", "1 cup yogurt", "400g tomato puree", "100g butter", "1 cup cream", "2 tbsp garam masala", "1 tbsp ginger-garlic paste" },
                Instructions = new() { "Marinate chicken in yogurt and spices for 1 hour.", "Grill or pan-sear the chicken until charred.", "Melt butter and saute ginger-garlic paste.", "Add tomato puree and simmer 10 minutes.", "Stir in cream and garam masala.", "Add the chicken and simmer until tender." } },
            new Recipe { Id = 6, Title = "Classic Cheeseburger", Cuisine = "American", PrepTime = 10, CookTime = 10, Servings = 4, Difficulty = "easy", Rating = 4.4, Image = "https://picsum.photos/seed/recipe6/600/400", Tags = new() { "grill", "beef", "quick" },
                Ingredients = new() { "600g ground beef", "4 burger buns", "4 slices cheddar", "1 tomato", "Lettuce leaves", "2 tbsp ketchup", "Salt and pepper" },
                Instructions = new() { "Form beef into 4 patties and season.", "Sear patties on a hot skillet 3 minutes per side.", "Add cheese and let it melt.", "Toast the buns lightly.", "Layer lettuce, tomato, and patty.", "Add ketchup and close the bun." } },
            new Recipe { Id = 7, Title = "Spaghetti Carbonara", Cuisine = "Italian", PrepTime = 10, CookTime = 15, Servings = 4, Difficulty = "medium", Rating = 4.7, Image = "https://picsum.photos/seed/recipe7/600/400", Tags = new() { "pasta", "classic", "quick" },
                Ingredients = new() { "400g spaghetti", "150g pancetta", "3 egg yolks", "1 whole egg", "80g pecorino romano", "Black pepper" },
                Instructions = new() { "Boil spaghetti in salted water until al dente.", "Crisp the pancetta in a pan.", "Whisk eggs with grated pecorino and pepper.", "Reserve pasta water, then drain.", "Toss hot pasta with pancetta off the heat.", "Stir in the egg mixture, loosening with pasta water." } },
            new Recipe { Id = 8, Title = "Vegetable Ramen", Cuisine = "Japanese", PrepTime = 20, CookTime = 25, Servings = 2, Difficulty = "hard", Rating = 4.3, Image = "https://picsum.photos/seed/recipe8/600/400", Tags = new() { "vegetarian", "soup", "comfort" },
                Ingredients = new() { "2 portions ramen noodles", "1L vegetable stock", "3 tbsp miso paste", "1 cup shiitake mushrooms", "2 baby bok choy", "1 sheet nori", "2 soft-boiled eggs" },
                Instructions = new() { "Simmer stock with miso paste.", "Saute mushrooms until golden.", "Blanch the bok choy.", "Cook the ramen noodles separately.", "Divide noodles into bowls and pour over broth.", "Top with mushrooms, bok choy, egg, and nori." } },
            new Recipe { Id = 9, Title = "Chicken Tikka Masala", Cuisine = "Indian", PrepTime = 30, CookTime = 25, Servings = 4, Difficulty = "medium", Rating = 4.8, Image = "https://picsum.photos/seed/recipe9/600/400", Tags = new() { "curry", "spicy", "comfort" },
                Ingredients = new() { "700g chicken breast", "1 cup yogurt", "400g crushed tomatoes", "1 cup cream", "2 tbsp tikka masala spice", "1 onion", "3 cloves garlic" },
                Instructions = new() { "Marinate chicken in yogurt and half the spice.", "Grill the chicken until charred.", "Saute onion and garlic until soft.", "Add tomatoes and remaining spice, simmer.", "Stir in cream and the grilled chicken.", "Simmer until the sauce thickens." } },
            new Recipe { Id = 10, Title = "Guacamole & Chips", Cuisine = "Mexican", PrepTime = 15, CookTime = 0, Servings = 6, Difficulty = "easy", Rating = 4.5, Image = "https://picsum.photos/seed/recipe10/600/400", Tags = new() { "vegetarian", "no-cook", "snack" },
                Ingredients = new() { "3 ripe avocados", "1 lime", "1 small onion", "1 tomato", "Fresh cilantro", "1 jalapeno", "Tortilla chips" },
                Instructions = new() { "Mash the avocados in a bowl.", "Squeeze in the lime juice.", "Finely dice onion, tomato, and jalapeno.", "Fold vegetables into the avocado.", "Season with salt and chopped cilantro.", "Serve with tortilla chips." } },
            new Recipe { Id = 11, Title = "BBQ Pulled Pork", Cuisine = "American", PrepTime = 20, CookTime = 240, Servings = 8, Difficulty = "hard", Rating = 4.6, Image = "https://picsum.photos/seed/recipe11/600/400", Tags = new() { "slow-cook", "pork", "bbq" },
                Ingredients = new() { "2kg pork shoulder", "2 tbsp brown sugar", "2 tbsp paprika", "1 cup BBQ sauce", "1 tbsp mustard", "Burger buns", "Coleslaw" },
                Instructions = new() { "Rub pork with sugar, paprika, and spices.", "Slow-cook at 120C for about 4 hours.", "Rest, then shred with two forks.", "Mix in the BBQ sauce and mustard.", "Pile onto buns.", "Top with coleslaw and serve." } },
            new Recipe { Id = 12, Title = "Mushroom Risotto", Cuisine = "Italian", PrepTime = 10, CookTime = 30, Servings = 4, Difficulty = "hard", Rating = 4.5, Image = "https://picsum.photos/seed/recipe12/600/400", Tags = new() { "vegetarian", "creamy", "rice" },
                Ingredients = new() { "300g arborio rice", "400g mixed mushrooms", "1L vegetable stock", "1 onion", "1/2 cup white wine", "50g parmesan", "2 tbsp butter" },
                Instructions = new() { "Saute mushrooms and set aside.", "Soften onion in butter.", "Toast the rice for 2 minutes.", "Deglaze with wine.", "Add warm stock one ladle at a time, stirring.", "Fold in mushrooms and parmesan to finish." } },
            new Recipe { Id = 13, Title = "Pad Thai", Cuisine = "Thai", PrepTime = 20, CookTime = 15, Servings = 3, Difficulty = "medium", Rating = 4.7, Image = "https://picsum.photos/seed/recipe13/600/400", Tags = new() { "noodles", "asian", "street-food" },
                Ingredients = new() { "200g rice noodles", "200g shrimp", "2 eggs", "1 cup bean sprouts", "3 tbsp tamarind paste", "2 tbsp fish sauce", "Crushed peanuts" },
                Instructions = new() { "Soak the rice noodles until pliable.", "Stir-fry shrimp until pink.", "Push aside and scramble the eggs.", "Add noodles and tamarind-fish sauce mix.", "Toss with bean sprouts.", "Serve topped with crushed peanuts and lime." } },
            new Recipe { Id = 14, Title = "Chicken Enchiladas", Cuisine = "Mexican", PrepTime = 25, CookTime = 25, Servings = 4, Difficulty = "medium", Rating = 4.4, Image = "https://picsum.photos/seed/recipe14/600/400", Tags = new() { "baked", "cheese", "comfort" },
                Ingredients = new() { "3 cooked chicken breasts", "8 corn tortillas", "2 cups enchilada sauce", "2 cups shredded cheese", "1 onion", "Fresh cilantro" },
                Instructions = new() { "Shred the cooked chicken.", "Mix chicken with onion and a little sauce.", "Roll the filling into tortillas.", "Place seam-side down in a baking dish.", "Cover with sauce and cheese.", "Bake at 190C for 25 minutes." } },
            new Recipe { Id = 15, Title = "Miso Soup", Cuisine = "Japanese", PrepTime = 5, CookTime = 10, Servings = 4, Difficulty = "easy", Rating = 4.2, Image = "https://picsum.photos/seed/recipe15/600/400", Tags = new() { "vegetarian", "soup", "quick" },
                Ingredients = new() { "1L dashi stock", "3 tbsp miso paste", "150g silken tofu", "2 tbsp wakame seaweed", "1 spring onion" },
                Instructions = new() { "Heat the dashi stock until warm.", "Whisk in the miso paste until dissolved.", "Add cubed tofu and wakame.", "Simmer gently for 2 minutes.", "Do not boil once miso is added.", "Garnish with sliced spring onion." } }
        };
    }
}
